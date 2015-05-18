using Stormancer.Core;
using Stormancer.Dto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

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
        /// A byte representing the handle of the scene for this peer in the cluster.
        /// </summary>
        /// <remarks>
        /// The handles is used internally by Stormancer to optimize bandwidth consumption.  Due to limitations in the handle format, clients can connect to 140 scenes simultaneously.
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
            _metadata = dto.Metadata;
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
        /// <param name="metadata">Optional metadata that will be assciated to the route</param>
        /// <returns></returns>
        public void AddRoute(string route, Action<Packet<IScenePeer>> handler, Dictionary<string, string> metadata = null)
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
                routeObj = new Route(this, route, metadata);
                _localRoutesMap.Add(route, routeObj);
            }

            OnMessage(route).Subscribe(handler);

        }

        /// <summary>
        /// Gets an observable that listen to the provided route
        /// </summary>
        /// <param name="route">A `Route` instance to listen to</param>
        /// <returns>An observable that provides all the packets received on the route</returns>
        public IObservable<Packet<IScenePeer>> OnMessage(Route route)
        {
            var index = route.Handle;
            var observable = Observable.Create<Packet<IScenePeer>>(observer =>
            {

                Action<Packet> action = (data) =>
                {
                    var packet = new Packet<IScenePeer>(Host, new SubStream(data.Stream, data.Stream.Length - data.Stream.Position, true), data.Metadata);
                    observer.OnNext(packet);
                };
                route.Handlers += action;


                return () =>
                {
                    route.Handlers -= action;
                };
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
        /// <param name="writer">An action that writes the packet content</param>
        /// <param name="priority">Priority level of the packet</param>
        /// <param name="reliability">Reliability level of the packet</param>
        /// <returns>A task completing when the transport takes</returns>
        public void SendPacket(string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY, PacketReliability reliability = PacketReliability.RELIABLE)
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

            _peer.SendToScene(this.Handle, routeObj.Handle, writer, priority, reliability);//.SendPacket(routeObj, writer, priority, reliability, channel);
        }




        /// <summary>
        /// Disconnects the scene.
        /// </summary>
        /// <returns></returns>
        public Task Disconnect()
        {
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
        public async Task Connect()
        {
            await this._client.ConnectToScene(this, this._token, this._localRoutesMap.Values);

            this.Connected = true;
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

        private IScenePeer _host;
        /// <summary>
        /// An `IScenePeer` object that represents the scene host.
        /// </summary>
        public IScenePeer Host
        {
            get
            {
                if (_host == null)
                {
                    _host = new ScenePeer(_peer, _handle, _remoteRoutesMap, this);
                }
                return _host;
            }
        }

        /// <summary>
        /// True if the scene is an host. False otherwise
        /// </summary>
        public bool IsHost
        {
            get { return false; }
        }


        private Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();

        /// <summary>
        /// Provides the component registered for a type
        /// </summary>
        /// <typeparam name="T">A component type</typeparam>
        /// <returns>The component object</returns>
        /// <remarks>
        /// usage: 
        /// 
        ///     var serializer = GetComponent&lt;ISerializer&gt;();
        /// </remarks>
        public T GetComponent<T>()
        {
            Func<object> factory;
            if (_registrations.TryGetValue(typeof(T), out factory))
            {
                return (T)factory();
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Registers a component with the scene object
        /// </summary>
        /// <typeparam name="T">The type under which the component will be registered</typeparam>
        /// <param name="component">A factory method that provides the component instance</param>
        /// <remarks>
        /// `T` T can be an interface implemented by the registered component
        /// </remarks>
        public void RegisterComponent<T>(Func<T> component)
        {
            _registrations[typeof(T)] = () => component();
        }
    }
}
