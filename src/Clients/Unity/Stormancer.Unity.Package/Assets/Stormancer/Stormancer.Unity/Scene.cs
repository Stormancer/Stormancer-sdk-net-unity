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
using Stormancer.Plugins;
using UniRx;
using Stormancer.Dto;

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

        private readonly Dictionary<string, string> _metadata;

        private readonly PluginBuildContext _pluginCtx;

        /// <summary>
        /// Returns metadata informations for the remote scene host.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetHostMetadata(string key)
        {
            string result = null;
            _metadata.TryGetValue(key, out result);
            return result;
        }

        /// <summary>
        /// A byte representing the index of the scene for this peer.
        /// </summary>
        /// <remarks>
        /// The index is used internally by Stormancer to optimize bandwidth consumption. That means that Stormancer clients can connect to only 256 scenes simultaneously.
        /// </remarks>
        public byte Handle { get { return _handle; } }

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

        public StormancerResolver DependencyResolver { get; private set; }

        internal Scene(IConnection connection, Client client, string id, string token, Stormancer.Dto.SceneInfosDto dto, PluginBuildContext pluginCtx, StormancerResolver res)
        {
            Id = id;
            this._peer = connection;
            _token = token;
            _client = client;
            _metadata = dto.Metadata;
            _pluginCtx = pluginCtx;
            DependencyResolver = new StormancerResolver(res);

            foreach (var route in dto.Routes)
            {
                _remoteRoutesMap.Add(route.Name, new Route(this, route.Name, route.Metadata) { Handle = route.Handle });
            }
        }


        /// <summary>
        /// Registers a route on the local peer.
        /// </summary>
        /// <param name="route">A string containing the name of the route to listen to.</param>
        /// <param name="handler">An action that is executed when the remote peer call the route.</param>
        /// <returns></returns>
        public void AddRoute(string route, Action<Packet<IScenePeer>> handler, Dictionary<string, string> metadata = null)
        {
            if (route[0] == '@')
            {
                DependencyResolver.Resolve<ILogger>().Log(Stormancer.Diagnostics.LogLevel.Error, this.Id, "AddRoute failed: Tried to create a route with the @ character");
                throw new ArgumentException("A route cannot start with the @ character.");
            }
            metadata = new Dictionary<string, string>();

            if (Connected)
            {
                DependencyResolver.Resolve<ILogger>().Error("AddRoute failed: Tried to create a route once connected");
                throw new InvalidOperationException("You cannot register handles once the scene is connected.");
            }

            Route routeObj;
            if (!_localRoutesMap.TryGetValue(route, out routeObj))
            {
                DependencyResolver.Resolve<ILogger>().Trace("Created route with id : '{0}'", route);
                routeObj = new Route(this, route, metadata);
                _localRoutesMap.Add(route, routeObj);
                var ev = _pluginCtx.RouteCreated;
                if (ev != null)
                {
                    ev(this, routeObj);
                }
            }

            OnMessage(route).Subscribe(handler);
        }

        public IObservable<Packet<IScenePeer>> OnMessage(Route route)
        {
            // var index = route.Handle;
            var observable = Observable.Create<Packet<IScenePeer>>(observer =>
            {

                Action<Packet> action = (data) =>
                {
                    var packet = new Packet<IScenePeer>(Host, data.Stream, data.Metadata);
                    observer.OnNext(packet);
                };
                route.Handlers += action;

                return UniRx.Disposable.Create(() =>
                {
                    route.Handlers -= action;
                });
            });
            return observable;
        }
        /// <summary>
        /// Creates an IObservable&lt;Packet&gt; instance that listen to events on the specified route.
        /// </summary>
        /// <param name="route">A string containing the name of the route to listen to.</param>
        /// <returns type="IObservable&lt;Packet&gt;">An IObservable&lt;Packet&gt; instance that fires each time a message is received on the route. </returns>
        public IObservable<Packet<IScenePeer>> OnMessage(string route)
        {
            if (Connected)
            {
                DependencyResolver.Resolve<ILogger>().Error("Tried rgister handles once connected");
                throw new InvalidOperationException("You cannot register handles once the scene is connected.");
            }

            Route routeObj;
            if (!_localRoutesMap.TryGetValue(route, out routeObj))
            {
                routeObj = new Route(this, route, new Dictionary<string, string>());
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
        public void SendPacket(string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY, PacketReliability reliability = PacketReliability.RELIABLE)
        {
            if (route == null)
            {
                DependencyResolver.Resolve<ILogger>().Error("SendPacket failed: Tried to send a meesage on null route");
                throw new ArgumentNullException("no route selected");
            }
            if (writer == null)
            {
                DependencyResolver.Resolve<ILogger>().Error("SendPacket failed: Tried to send message with a null writer");
                throw new ArgumentNullException("no writer given");
            }
            if (!this.Connected)
            {
                DependencyResolver.Resolve<ILogger>().Error("SendPacket failed: Tried to send message without being connected");
                throw new InvalidOperationException("The scene must be connected to perform this operation.");
            }
            Route routeObj;
            if (!_remoteRoutesMap.TryGetValue(route, out routeObj))
            {
                DependencyResolver.Resolve<ILogger>().Error("SendPacket failed: The route '{0}' doesn't exist on the scene.", route);
                throw new ArgumentException("The route " + route + " doesn't exist on the scene.");
            }

            _peer.SendToScene(this.Handle, routeObj.Handle, writer, priority, reliability);//.SendPacket(routeObj, writer, priority, reliability, channel);
        }

        /// <summary>
        /// Disconnects the scene.
        /// </summary>
        /// <returns></returns>
        public Task Disconnect()
        {
            DependencyResolver.Resolve<ILogger>().Trace("Client disconnected from the server");
            return this._client.Disconnect(this, this._handle);
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
            return this._client.ConnectToScene(this, this._token, this._localRoutesMap.Values)
                .Then(() =>
                {
                    DependencyResolver.Resolve<ILogger>().Info("Successfully connected to scene : '{0}'.", Id);
                    this.Connected = true;

                    _client.OnDisconnected += reason => this.Connected = false;
                });
        }

        internal void CompleteConnectionInitialization(ConnectionResult cr)
        {
            this._handle = cr.SceneHandle;

            foreach (var route in _localRoutesMap)
            {
                route.Value.Handle = cr.RouteMappings[route.Key];
                _handlers.TryAdd(route.Value.Handle, route.Value.Handlers);
            }
        }

        /// <summary>
        /// Fires when packets are received on the scene.
        /// </summary>
        public Action<Packet> PacketReceived;

        private Client _client;

        internal void HandleMessage(Packet packet)
        {

            var temp = new byte[2];
            //Extract the route id.
            packet.Stream.Read(temp, 0, 2);
            var routeId = BitConverter.ToUInt16(temp, 0);

            packet.Metadata["routeId"] = routeId;
            var ev = _pluginCtx.PacketReceived;
            if (ev != null)
            {
                ev(packet);
            }
            Action<Packet> observer;

            if (_handlers.TryGetValue(routeId, out observer))
            {
                try
                {
                    observer(packet);
                }
                catch (Exception ex)
                {
                    DependencyResolver.Resolve<ILogger>().Log(Diagnostics.LogLevel.Error, "scene.route", "An exception occurred while trying to process a route message", ex);
                    throw;
                }
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
                return new ScenePeer(_peer, _handle, _remoteRoutesMap, this);
            }
        }

        public bool IsHost
        {
            get { return false; }
        }
    }
}