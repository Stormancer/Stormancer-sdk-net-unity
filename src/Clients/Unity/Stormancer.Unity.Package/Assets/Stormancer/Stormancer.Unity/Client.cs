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
        private readonly ApiClient _apiClient;
        private readonly string _accountId;
        private readonly string _applicationName;

        private readonly PluginBuildContext _pluginCtx = new PluginBuildContext();
        private IConnection _serverConnection;

        private ITransport _transport;
        private IPacketDispatcher _dispatcher;

        private bool _initialized;

        private ITokenHandler _tokenHandler = new TokenHandler();

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

        private ILogger _logger = NullLogger.Instance;

        /// <summary>
        /// An user specified logger.
        /// </summary>
        public ILogger Logger
        {
            get
            {
                return _logger;
            }
            set
            {
                if (value == null)
                {
                    _logger = NullLogger.Instance;
                }
                else
                {
                    _logger = value;
                }
            }
        }

        /// <summary>
        /// Creates a Stormancer client instance.
        /// </summary>
        /// <param name="configuration">A configuration instance containing options for the client.</param>
        public Client(ClientConfiguration configuration)
        {
            this._logger = configuration.Logger;
            this._accountId = configuration.Account;
            this._applicationName = configuration.Application;
            _apiClient = new ApiClient(configuration, _tokenHandler);
            this._transport = configuration.TransportFactory(new Dictionary<string, object> { { "ILogger", this._logger } });
            this._dispatcher = configuration.Dispatcher;
            _requestProcessor = new Stormancer.Networking.Processors.RequestProcessor(_logger, Enumerable.Empty<IRequestModule>());

            _scenesDispatcher = new Processors.SceneDispatcher();
            this._dispatcher.AddProcessor(_requestProcessor);
            this._dispatcher.AddProcessor(_scenesDispatcher);
            this._metadata = configuration._metadata;

            foreach (var serializer in configuration.Serializers)
            {
                this._serializers.Add(serializer.Name, serializer);
            }

            this._metadata.Add("serializers", string.Join(",", this._serializers.Keys.ToArray()));
            this._metadata.Add("transport", _transport.Name);
            this._metadata.Add("version", "1.0.0a");
            this._metadata.Add("platform", "Unity");
            this._metadata.Add("protocol", "2");

            this._maxPeers = configuration.MaxPeers;

            foreach (var plugin in configuration.Plugins)
            {
                plugin.Build(_pluginCtx);
            }

            if (_pluginCtx.ClientCreated != null)
            {
                _pluginCtx.ClientCreated(this);
            }

            Initialize();
        }

        private void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;

                _transport.PacketReceived += Transport_PacketReceived;


            }
        }

        private void Transport_PacketReceived(Stormancer.Core.Packet obj)
        {
            if (_pluginCtx.PacketReceived != null)
            {
                _pluginCtx.PacketReceived(obj);
            }

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
            return _apiClient.GetSceneEndpoint(this._accountId, this._applicationName, sceneId, userData)
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
                        return _transport.Connect(ci.TokenData.Endpoints[_transport.Name])
                            .Then(connection =>
                            {
                                _serverConnection = connection;

                                foreach (var kvp in _metadata)
                                {
                                    _serverConnection.Metadata[kvp.Key] = kvp.Value;
                                }
                                return this.UpdateServerMetadata();
                            });
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
                        throw new InvalidOperationException("No serializer selected.");
                    }
                    _serverConnection.RegisterComponent(_serializers[result.SelectedSerializer]);
                    _serverConnection.Metadata.Add("serializer", result.SelectedSerializer);
                }
                return UpdateServerMetadata().Then(() =>
                {
                    var scene = new Scene(this._serverConnection, this, sceneId, ci.Token, result);

                    if (_pluginCtx.SceneCreated != null)
                    {
                        _pluginCtx.SceneCreated(scene);
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
            var ci = _tokenHandler.DecodeToken(token);
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
                        var result = t.Result;
                        this.Logger.Trace("Received connection result. Scene handle: {0}", result.SceneHandle);
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
            if (_serverConnection != null)
            {
                _serverConnection.Close();
            }

        }

        private bool _disposed;

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
