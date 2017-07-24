using MsgPack.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Stormancer.Client45.Infrastructure;
using Stormancer.Client45;
using Stormancer.Networking;
using Stormancer.Core;
using Stormancer.Cluster.Application;
using Stormancer.Plugins;
using System.Diagnostics;

using UnityEngine;

namespace Stormancer
{
    /// <summary>
    /// Stormancer client library
    /// </summary>
    public class Client : IDisposable
    {
        private class ConnectionHandler : IConnectionManager
        {
            private long _current = 0;
            public long GenerateNewConnectionId()
            {
                lock (this)
                {
                    return _current++;
                }
            }

            public void NewConnection(IConnection connection)
            {

            }

            public IConnection GetConnection(long id)
            {
                throw new NotImplementedException();
            }

            public void CloseConnection(IConnection connection, string reason)
            {

            }
        }

        private readonly string _accountId;
        private readonly string _applicationName;
        private readonly int _pingInterval = 5000;

        private readonly PluginBuildContext _pluginCtx = new PluginBuildContext();
        private IConnection _serverConnection;


        private IPacketDispatcher _dispatcher;

        private bool _initialized;


        private readonly ISerializer _systemSerializer = new MsgPackMapSerializer();

        private Stormancer.Networking.Processors.RequestProcessor _requestProcessor;
        private Stormancer.Processors.SceneDispatcher _scenesDispatcher;
        private Dictionary<string, ISerializer> _serializers = new Dictionary<string, ISerializer>();


        private CancellationTokenSource _cts;
        private ushort _maxPeers;

        private Dictionary<string, string> _metadata;
        /// <summary>
        /// The name of the Stormancer server application the client is connected to.
        /// </summary>
        public string ApplicationName
        {
            get
            {
                return this._applicationName;
            }
        }


        private readonly IScheduler _scheduler;
        public StormancerResolver DependencyResolver { get; private set; }

        /// <summary>
        /// An user specified logger.
        /// </summary>
        public ILogger Logger
        {
            get
            {
                return DependencyResolver.Resolve<ILogger>();
            }

        }

        public event Action<string> OnDisconnected;

        /// <summary>
        /// Creates a Stormancer client instance.
        /// </summary>
        /// <param name="configuration">A configuration instance containing options for the client.</param>
        public Client(ClientConfiguration configuration)
        {
            this._pingInterval = configuration.PingInterval;
            this._scheduler = configuration.Scheduler;
            DependencyResolver = new StormancerResolver();
            DependencyResolver.Register<ILogger>(() => configuration.Logger);
            DependencyResolver.Register(() => new ApiClient(configuration, DependencyResolver));
            DependencyResolver.Register<ITokenHandler>(() => new TokenHandler());
            DependencyResolver.RegisterComponent<IConnectionHandler>(new IConnectionHandler());
            DependencyResolver.RegisterComponent<IClock>(new IClock(this));

#if UNITY_EDITOR
            IConnectionHandler temp = DependencyResolver.Resolve<IConnectionHandler>();
            temp.PeerConnected += (PeerConnectedContext pcc) =>
            {
                ConnectionWrapper connection = new ConnectionWrapper(pcc.Connection, configuration.Plugins.OfType<EditorPlugin.StormancerEditorPlugin>().First());
                pcc.Connection = connection;
            };
#endif

            this.DependencyResolver.Register<ITransport>(configuration.TransportFactory);
            this._accountId = configuration.Account;
            this._applicationName = configuration.Application;
            //TODO handle scheduler in the transport
            this._dispatcher = configuration.Dispatcher;
            _requestProcessor = new Stormancer.Networking.Processors.RequestProcessor(Logger, Enumerable.Empty<IRequestModule>());

            _scenesDispatcher = new Processors.SceneDispatcher();
            this._dispatcher.AddProcessor(_requestProcessor);
            this._dispatcher.AddProcessor(_scenesDispatcher);
            this._metadata = configuration._metadata;

            foreach (var serializer in configuration.Serializers)
            {
                this._serializers.Add(serializer.Name, serializer);
            }

            this._maxPeers = configuration.MaxPeers;

            foreach (var plugin in configuration.Plugins)
            {
                plugin.Build(_pluginCtx);
            }

            var ev = _pluginCtx.ClientCreated;
            if (ev != null)
            {
                ev(this);
            }

            _transport = DependencyResolver.Resolve<ITransport>();
            this._metadata.Add("serializers", string.Join(",", this._serializers.Keys.ToArray()));
            this._metadata.Add("transport", _transport.Name);
            this._metadata.Add("version", "1.1.0");
            this._metadata.Add("platform", "Unity");
            this._metadata.Add("protocol", "2");

            Initialize();
        }

        private Stopwatch _watch = new Stopwatch();

        /// <summary>
        /// Synchronized clock with the server.
        /// </summary>
        public long Clock
        {
            get
            {
                return _watch.ElapsedMilliseconds + _offset;
            }
        }

        /// <summary>
        /// Last ping value with the cluster.
        /// </summary>
        /// <remarks>
        /// 0 means that no mesure has be made yet.
        /// </remarks>
        public long LastPing
        {
            get;
            private set;
        }
        private long _offset;

        private void Initialize()
        {
            if (!_initialized)
            {
                this.Logger.Trace("creating client");
                _initialized = true;

                _transport.PacketReceived += Transport_PacketReceived;

                this._watch.Start();
            }
        }

        private void Transport_PacketReceived(Stormancer.Core.Packet obj)
        {
            //if (_pluginCtx.PacketReceived != null)
            //{
            //    _pluginCtx.PacketReceived(obj);
            //}

            _dispatcher.DispatchPacket(obj);
        }


        /// <summary>
        /// Returns a public scene (accessible without authentication)
        /// </summary>
        /// <remarks>
        /// The effective connection happens when "Connect" is called on the scene.
        /// </remarks>
        /// <param name="sceneId">The id of the scene to connect to.</param>
        /// <param name="userData">User data that should be associated to the connection.</param>
        /// <returns>A task returning the scene</returns>
        public Task<Scene> GetPublicScene<T>(string sceneId, T userData)
        {
            return DependencyResolver.Resolve<ApiClient>().GetSceneEndpoint(this._accountId, this._applicationName, sceneId, userData)
                .Then(ci => GetScene(sceneId, ci));
        }

        private Task<U> SendSystemRequest<T, U>(byte id, T parameter)
        {
            return _requestProcessor.SendSystemRequest(_serverConnection, id, s =>
            {
                _systemSerializer.Serialize(parameter, s);
            }).Then(packet => _systemSerializer.Deserialize<U>(packet.Stream));
        }

        private Task UpdateServerMetadata()
        {
            return _requestProcessor.SendSystemRequest(_serverConnection, (byte)SystemRequestIDTypes.ID_SET_METADATA, s =>
            {
                _systemSerializer.Serialize(_serverConnection.Metadata, s);
            });
        }

        /// <summary>
        /// Returns a private scene (requires a token obtained from strong authentication with the Stormancer API).
        /// </summary>
        /// <remarks>
        /// The effective connection happens when "Connect" is called on the scene. Note that when you call GetScene, 
        /// a connection token is requested from the Stormancer API.this token is only valid for a few minutes: Don't get scenes
        /// a long time before connecting to them.
        /// </remarks>
        /// <param name="token">The token securing the connection.</param>
        /// <returns>A task returning the scene object on completion.</returns>        
        private Task<Scene> GetScene(string sceneId, SceneEndpoint ci)
        {
            return TaskHelper.If(_serverConnection == null, () =>
            {
                return TaskHelper.If(!_transport.IsRunning, () =>
                {
                    _cts = new CancellationTokenSource();
                    return _transport.Start("client", new ConnectionHandler(), _cts.Token, null, (ushort)(_maxPeers + 1));
                })
                    .Then(() =>
                    {
                        UnityEngine.Debug.Log("Connecting transport...");
                        return _transport.Connect(ci.TokenData.Endpoints[_transport.Name]);
                    })
                    .Then(connection =>
                    {
                        this.Logger.Log(Stormancer.Diagnostics.LogLevel.Trace, sceneId, string.Format("Trying to connect to scene '{0}' through endpoint : '{1}' on application : '{2}' with id : '{3}'", sceneId, ci.TokenData.Endpoints[_transport.Name], ci.TokenData.Application, ci.TokenData.AccountId));
                        _serverConnection = connection;
                        connection.ConnectionClosed += reason =>
                        {
                            var action = OnDisconnected;
                            if (action != null)
                            {
                                action(reason);
                            }
                        };

                        foreach (var kvp in _metadata)
                        {
                            _serverConnection.Metadata[kvp.Key] = kvp.Value;
                        }
                        return this.UpdateServerMetadata();
                    })
                    .Then(() =>
                    {
                        if (ci.TokenData.Version > 0)
                        {
                            StartSyncClock();
                        }
                    });
            }).Then(() =>
            {
                var parameter = new Stormancer.Dto.SceneInfosRequestDto { Metadata = _serverConnection.Metadata, Token = ci.Token };
                return SendSystemRequest<Stormancer.Dto.SceneInfosRequestDto, Stormancer.Dto.SceneInfosDto>((byte)SystemRequestIDTypes.ID_GET_SCENE_INFOS, parameter);
            }).Then(result =>
            {
                if (_serverConnection.GetComponent<ISerializer>() == null)
                {
                    if (result.SelectedSerializer == null)
                    {
                        this.Logger.Error("No serializer selected");
                        throw new InvalidOperationException("No serializer selected.");
                    }
                    _serverConnection.RegisterComponent(_serializers[result.SelectedSerializer]);
                    _serverConnection.Metadata.Add("serializer", result.SelectedSerializer);
                    this.Logger.Info("Serializer selected: " + result.SelectedSerializer);
                }
                return UpdateServerMetadata().Then(() =>
                {
                    var scene = new Scene(this._serverConnection, this, sceneId, ci.Token, result, _pluginCtx, DependencyResolver);

                    var ev = _pluginCtx.SceneCreated;
                    if (ev != null)
                    {
                        ev(scene);
                    }

                    return scene;
                });
            });


            //if (_serverConnection == null)
            //{
            //    if (!_transport.IsRunning)
            //    {
            //        cts = new CancellationTokenSource();
            //        await _transport.Start("client", new ConnectionHandler(), cts.Token, null, (ushort)(_maxPeers + 1));
            //    }
            //    _serverConnection = await _transport.Connect(ci.TokenData.Endpoints[_transport.Name]);
            //}


            //var parameter = new Stormancer.Dto.SceneInfosRequestDto { Metadata = _metadata, Token = ci.Token };

            //var result = await SendSystemRequest<Stormancer.Dto.SceneInfosRequestDto, Stormancer.Dto.SceneInfosDto>((byte)MessageIDTypes.ID_GET_SCENE_INFOS, parameter);

            //if (!_serverConnection.Components.ContainsKey("serializer"))
            //{
            //    if (result.SelectedSerializer == null)
            //    {
            //        throw new InvalidOperationException("No seralizer selected.");
            //    }
            //    _serverConnection.Components["serializer"] = _serializers[result.SelectedSerializer];
            //}
            //var scene = new Scene(this._serverConnection, this, sceneId, ci.Token, result);
            //_scenesDispatcher.AddScene(scene);
            //return scene;
        }

        private IDisposable _syncClockSubscription;
        private void StartSyncClock()
        {
            if (this._syncClockSubscription != null)
            {
                return;
            }
            this._syncClockSubscription = this._scheduler.SchedulePeriodic(this._pingInterval, () =>
            {
                this.SyncClockImpl();
            });
        }

        private Task SyncClockImpl()
        {
            long timeStart = this._watch.ElapsedMilliseconds;

            try
            {
                return this._requestProcessor.SendSystemRequest(this._serverConnection, (byte)SystemRequestIDTypes.ID_PING, s =>
                {
                    s.Write(BitConverter.GetBytes(timeStart), 0, 8);
                }, PacketPriority.IMMEDIATE_PRIORITY)
                .Then(response =>
                {
                    ulong timeRef = 0;
                    var timeEnd = this._watch.ElapsedMilliseconds;

                    for (var i = 0; i < 8; i++)
                    {
                        timeRef += (((ulong)response.Stream.ReadByte()) << (8 * i));
                    }

                    this.LastPing = timeEnd - timeStart;
                    this._offset = (long)timeRef - (this.LastPing / 2) - timeStart;
                })
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Logger.Log(Stormancer.Diagnostics.LogLevel.Error, "ping", "failed to ping server.");
                    }
                });
            }
            catch (Exception)
            {
                Logger.Log(Stormancer.Diagnostics.LogLevel.Error, "ping", "failed to ping server.");
                return TaskHelper.FromResult(UniRx.Unit.Default);
            };
        }


        /// <summary>
        /// Returns a private scene (requires a token obtained from strong authentication with the Stormancer API.
        /// </summary>
        /// <remarks>
        /// The effective connection happens when "Connect" is called on the scene. Note that when you call GetScene, 
        /// a connection token is requested from the Stormancer API.this token is only valid for a few minutes: Don't get scenes
        /// a long time before connecting to them.
        /// </remarks>
        /// <param name="token">The token securing the connection.</param>
        /// <returns>A task returning the scene object on completion.</returns>
        public Task<Scene> GetScene(string token)
        {
            var ci = DependencyResolver.Resolve<ITokenHandler>().DecodeToken(token);
            return GetScene(ci.TokenData.SceneId, ci);
        }

        internal Task ConnectToScene(Scene scene, string token, IEnumerable<Route> localRoutes)
        {
            var parameter = new Stormancer.Dto.ConnectToSceneMsg
            {
                Token = token,
                Routes = localRoutes.Select(r => new Stormancer.Dto.RouteDto
                {
                    Handle = r.Handle,
                    Metadata = r.Metadata,
                    Name = r.Name
                }).ToList(),
                ConnectionMetadata = _serverConnection.Metadata
            };
            return this.SendSystemRequest<Stormancer.Dto.ConnectToSceneMsg, Stormancer.Dto.ConnectionResult>((byte)SystemRequestIDTypes.ID_CONNECT_TO_SCENE, parameter)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        var ex = t.Exception.InnerException;
                        this.Logger.Log(Stormancer.Diagnostics.LogLevel.Error, scene.Id, string.Format("Failed to connect to scene '{0}' : {1}", scene.Id, ex.Message));
                        throw t.Exception.InnerException;

                    }
                    var result = t.Result;
                    this.Logger.Log(Stormancer.Diagnostics.LogLevel.Trace, scene.Id, string.Format("Received connection result. Scene handle: {0}", result.SceneHandle));
                    scene.CompleteConnectionInitialization(result);
                    _scenesDispatcher.AddScene(scene);
                    if (_pluginCtx.SceneConnected != null)
                    {
                        _pluginCtx.SceneConnected(scene);
                    }
                });
        }


        internal Task Disconnect(Scene scene, byte sceneHandle)
        {
            return this.SendSystemRequest<byte, Stormancer.Dto.Empty>((byte)SystemRequestIDTypes.ID_DISCONNECT_FROM_SCENE, sceneHandle)
                .Then(() =>
                {
                    this._scenesDispatcher.RemoveScene(sceneHandle);
                    if (_pluginCtx.SceneDisconnected != null)
                    {
                        _pluginCtx.SceneDisconnected(scene);
                    }
                });
        }


        /// <summary>
        /// Disconnects the client.
        /// </summary>
        public void Disconnect()
        {
            using (this._syncClockSubscription)
            { };
            if (_serverConnection != null)
            {
                _serverConnection.Close();
            }

        }

        private bool _disposed;
        private ITransport _transport;

        /// <summary>
        /// Disposes the client object.
        /// </summary>
        /// <remarks>
        /// Calls the *Disconnect* method  to shutdown the transport gracefully.
        /// </remarks>
        public void Dispose()
        {
            if (!this._disposed)
            {
                this._disposed = true;
                Disconnect();

                var ev = _pluginCtx.ClientDestroyed;
                if (ev != null)
                {
                    ev(this);
                }
            }

        }





        //internal IObservable<Packet> SendRequest(IConnection peer, byte scene, ushort route, Action<Stream> writer)
        //{
        //    if (writer == null)
        //    {
        //        throw new ArgumentNullException("writer");

        //    }
        //    return _requestProcessor.SendSceneRequest(peer, scene, route, writer);
        //}

        /// <summary>
        /// The client's unique stormancer Id. Returns null if the Id has not been acquired yet (connection still in progress).
        /// </summary>
        public long? Id { get { return this._transport.Id; } }

        /// <summary>
        /// The server connection's ping, in milliseconds.
        /// </summary>
        public int ServerPing { get { return this._serverConnection.Ping; } }

        /// <summary>
        /// The name of the transport used for connecting to the server.
        /// </summary>
        public string ServerTransportType { get { return this._transport.Name; } }

        /// <summary>
        /// Returns statistics about the connection to the server.
        /// </summary>
        /// <returns>The required statistics</returns>
        public IConnectionStatistics GetServerConnectionStatistics()
        {
            return this._serverConnection.GetConnectionStatistics();
        }
    }
}
