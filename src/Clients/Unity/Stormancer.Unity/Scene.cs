using MsgPack.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Core.Infrastructure.Messages;
using System.IO;
using Stormancer.Client45.Infrastructure;
using Stormancer.Networking;
using Stormancer.Core;
using UniRx;

namespace Stormancer
{
    /// <summary>
    /// Represents a clientside Stormancer scene.
    /// </summary>
    /// <remarks>
    /// Scenes are created by Stormancer clients through the <see cref="Stormancer.Client.GetScene"/> and <see cref="Stormancer.Client.GetPublicScene"/> methods.
    /// </remarks>
    public class Scene : IScene
    {
        private readonly IConnection _peer;
        private string _token;


        private byte _handle;

        /// <summary>
        /// A byte representing the index of the scene for this peer.
        /// </summary>
        /// <remarks>
        /// The index is used internally by Stormancer to optimize bandwidth consumption. That means that Stormancer clients can connect to only 256 scenes simultaneously.
        /// </remarks>
        public byte Handle { get { return _handle; } }

        private ushort _maxRouteRegistration;

        /// <summary>
        /// A string representing the unique Id of the scene.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// A boolean representing whether the scene is connected or not.
        /// </summary>
        public bool Connected { get; private set; }

        private Dictionary<string, Route> _localRoutesMap = new Dictionary<string, Route>();
        private Dictionary<string, Route> _remoteRoutesMap = new Dictionary<string, Route>();

        private ConcurrentDictionary<ushort, Action<Packet>> _handlers = new ConcurrentDictionary<ushort, Action<Packet>>();

        public IConnection HostConnection { get { return _peer; } }
        /// <summary>
        /// Returns a list of the routes registered on the local peer.
        /// </summary>
        public IEnumerable<Route> LocalRoutes
        {
            get
            {
                return _localRoutesMap.Values;
            }
        }

        /// <summary>
        /// Returns a list of the routes available on the remote peer.
        /// </summary>
        public IEnumerable<Route> RemoteRoutes
        {
            get
            {
                return _remoteRoutesMap.Values;
            }
        }

        internal Scene(IConnection connection, Client client, string id, string token, Stormancer.Dto.SceneInfosDto dto)
        {
            Id = id;
            this._peer = connection;
            _token = token;
            _client = client;

            foreach (var route in dto.Routes)
            {
                _remoteRoutesMap.Add(route.Name, new Route(this, route.Name, route.Handle, route.Metadata));
            }
        }


        /// <summary>
        /// Registers a route on the local peer.
        /// </summary>
        /// <param name="route">A string containing the name of the route to listen to.</param>
        /// <param name="handler">An action that is executed when the remote peer call the route.</param>
        /// <returns></returns>
        public void AddRoute(string route, Action<Packet> handler, Dictionary<string, string> metadata = null)
        {
            if (route[0] == '@')
            {
                throw new ArgumentException("A route cannot start with the @ character.");
            }
            metadata = new Dictionary<string, string>();

            if (Connected)
            {
                throw new InvalidOperationException("You cannot register handles once the scene is connected.");
            }

            Route routeObj;
            if (!_localRoutesMap.TryGetValue(route, out routeObj))
            {
                routeObj = new Route(this, route, _maxRouteRegistration++, metadata);
                _localRoutesMap.Add(route, routeObj);
            }

            OnMessage(route).Subscribe(handler);

        }

        public IObservable<Packet> OnMessage(Route route)
        {
            var index = route.Index;
            var observable = Observable.Create<Packet>(observer =>
            {

                Action<Packet> action = (data) =>
                {

                    observer.OnNext(data);
                };
                _handlers.AddOrUpdate(index, action, (_, existingAction) =>
                {
                    existingAction += action;
                    return existingAction;
                });

                return () =>
                {
                    Action<Packet> existingAction;
                    if (_handlers.TryGetValue(index, out existingAction))
                    {
                        existingAction -= action;
                    }
                };
            });
            return observable;
        }
        /// <summary>
        /// Creates an IObservable&lt;Packet&gt; instance that listen to events on the specified route.
        /// </summary>
        /// <param name="route">A string containing the name of the route to listen to.</param>
        /// <returns type="IObservable&lt;Packet&gt;">An IObservable&lt;Packet&gt; instance that fires each time a message is received on the route. </returns>
        public IObservable<Packet> OnMessage(string route)
        {
            if (Connected)
            {
                throw new InvalidOperationException("You cannot register handles once the scene is connected.");
            }

            Route routeObj;
            if (!_localRoutesMap.TryGetValue(route, out routeObj))
            {
                routeObj = new Route(this, route, _maxRouteRegistration++, null);
                _localRoutesMap.Add(route, routeObj);
            }
            return OnMessage(routeObj);

        }

        /// <summary>
        /// Sends a packet to the scene.
        /// </summary>
        /// <param name="route">A string containing the route on which the message should be sent.</param>
        /// <param name="writer">An action called.</param>
        /// <returns>A task completing when the transport takes</returns>
        public void SendPacket(string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY, PacketReliability reliability = PacketReliability.RELIABLE, char channel = (char)0)
        {
            if (route == null)
            {
                throw new ArgumentNullException("route");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (!this.Connected)
            {
                throw new InvalidOperationException("The scene must be connected to perform this operation.");
            }
            Route routeObj;
            if (!_remoteRoutesMap.TryGetValue(route, out routeObj))
            {
                throw new ArgumentException("The route doesn't exist on the scene.");
            }

            _peer.SendToScene(this.Handle, routeObj.Index, writer, priority, reliability, channel);//.SendPacket(routeObj, writer, priority, reliability, channel);
        }


        /// <summary>
        /// Sends a request to the remote peer with ability to get responses.
        /// </summary>
        /// <param name="route">The remote route on wich to send the request.</param>
        /// <param name="writer">An `Action&lt;Stream&gt;` object that will write the request content.</param>
        /// <returns>An observable notifying responses from the remote host.</returns>
        public IObservable<Packet> SendRequest(string route, Action<Stream> writer)
        {

            return _client.SendRequest(this._peer, this._handle, this._remoteRoutesMap[route].Index, writer);
        }



        /// <summary>
        /// Disconnects the scene.
        /// </summary>
        /// <returns></returns>
        public Task Disconnect()
        {
            return this._client.Disconnect(this._handle);
            //var sysResponse = await this._client.SendWithResponse(Mess, "scene.stop", this.Id)
            //    //Handles if the server sends no response
            //    .DefaultIfEmpty(default(SystemResponse))
            //    // Adds a timeout
            //    .Amb(Observable.Throw<SystemResponse>(new TimeoutException()).DelaySubscription(TimeSpan.FromMilliseconds(5000)));

            //if (sysResponse != null && sysResponse.IsError)
            //{
            //    throw new Exception(sysResponse.Message);
            //}

            //foreach (var handler in _handlers)
            //    this.Connected = false;
        }

        /// <summary>
        /// Connects the scene to the server.
        /// </summary>
        /// <returns>A task completed once the connection is complete.</returns>
        /// <remarks>
        /// The task is susceptible to throw an exception in case of connection error.
        /// </remarks>
        public Task Connect()
        {
            return this._client.ConnectToScene(this._token, this._localRoutesMap.Values)
                .ContinueWith(t => {
                    this._handle = t.Result;
                    this.Connected = true;
                });
        }

        /// <summary>
        /// Fires when packet are received on the scene.
        /// </summary>
        /// 
        public Action<Packet> PacketReceived;
        private Client _client;


        internal void HandleMessage(Packet packet)
        {


            var ev = PacketReceived;
            if (ev != null)
            {
                ev(packet);
            }
            var temp = new byte[2];
            //Extract the route id.
            packet.Stream.Read(temp, 0, 2);
            var routeId = BitConverter.ToUInt16(temp, 0);


            packet.Metadata["routeId"] = routeId;
            packet.Metadata["scene"] = this;

            Action<Packet> observer;

            if (_handlers.TryGetValue(routeId, out observer))
            {
                observer(packet);
            }

        }

        /// <summary>
        /// List containing the scene host connection.
        /// </summary>
        public IEnumerable<IScenePeer> RemotePeers
        {
            get
            {
                return new IScenePeer[] { Host };
            }
        }

        public IScenePeer Host
        {
            get
            {
                return new ScenePeer(_peer, _handle, _remoteRoutesMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Index), this);
            }
        }

        public bool IsHost
        {
            get { return false; }
        }
    }
}
