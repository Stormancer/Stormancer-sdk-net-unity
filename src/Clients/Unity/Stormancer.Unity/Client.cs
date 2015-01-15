using MsgPack.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using System.Text;
using System.Threading.Tasks;
#if StormancerClient
#else
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR;
#endif
using System.Threading;
using Stormancer.Proxy.Agent;
using System.IO;
using Stormancer.Client45.Infrastructure;
using Stormancer.Client45;
using Stormancer.Networking;
using Stormancer.Core;
using Stormancer.Cluster.Application;


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
                return _current++;
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

        private IConnection _serverConnection;

        private ITransport _transport;
        private IPacketDispatcher _dispatcher;

        private bool _initialized;

        private ITokenHandler _tokenHandler = new TokenHandler();

        private ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingTasksTcs = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        private readonly ISerializer _systemSerializer = new MsgPackSerializer();

        private Stormancer.Networking.Processors.RequestProcessor _requestProcessor;
        private Stormancer.Processors.SceneDispatcher _scenesDispatcher;
        private Dictionary<string, ISerializer> _serializers = new Dictionary<string, ISerializer>();


        private CancellationTokenSource cts;
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
            this._accountId = configuration.Account;
            this._applicationName = configuration.Application;
            _apiClient = new ApiClient(configuration, _tokenHandler);
            this._transport = configuration.Transport;
            this._dispatcher = configuration.Dispatcher;
            _requestProcessor = new Stormancer.Networking.Processors.RequestProcessor(_logger, new List<IRequestModule>());

            _scenesDispatcher = new Processors.SceneDispatcher();
            this._dispatcher.AddPRocessor(_requestProcessor);
            this._dispatcher.AddPRocessor(_scenesDispatcher);
            this._metadata = configuration._metadata;

            foreach (var serializer in configuration.Serializers)
            {
                this._serializers.Add(serializer.Name, serializer);
            }

            this._metadata.Add("serializers", string.Join(",", this._serializers.Keys.ToArray()));
            this._metadata.Add("transport", _transport.Name);

            this._maxPeers = configuration.MaxPeers;

            Initialize();
        }

        private void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;

                _transport.PacketReceived += _transport_PacketReceived;


            }
        }

        void _transport_PacketReceived(Stormancer.Core.Packet obj)
        {

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
                .ContinueWith(t =>
                    {
                        var ci = t.Result;
                        return GetScene(sceneId, ci);
                    }
            ).Unwrap();
        }

        private Task<U> SendSystemRequest<T, U>(byte id, T parameter)
        {
            var tcs = new TaskCompletionSource<U>();

            _requestProcessor.SendSystemRequest(_serverConnection, id, s =>
            {
                _systemSerializer.Serialize(parameter, s);
            }).Subscribe(packet => tcs.SetResult(_systemSerializer.Deserialize<U>(packet.Stream)));

            return tcs.Task;
        }
        private Task<Scene> GetScene(string sceneId, SceneEndpoint ci)
        {
            return TaskHelper.If(_serverConnection == null, () =>
            {
                return TaskHelper.If(!_transport.IsRunning, () =>
                    {
                        cts = new CancellationTokenSource();
                        return _transport.Start("client", new ConnectionHandler(), cts.Token, null, (ushort)(_maxPeers + 1));
                    })
                    .Then(() =>
                    {
                        return _transport.Connect(ci.TokenData.Endpoints[_transport.Name])
                            .Then(connection =>
                            {
                                _serverConnection = connection;
                            });
                    });
            }).Then(() =>
            {
                var parameter = new Stormancer.Dto.SceneInfosRequestDto { Metadata = _metadata, Token = ci.Token };
                return SendSystemRequest<Stormancer.Dto.SceneInfosRequestDto, Stormancer.Dto.SceneInfosDto>((byte)MessageIDTypes.ID_GET_SCENE_INFOS, parameter);
            }).Then(result =>
            {
                if (!_serverConnection.Components.ContainsKey("serializer"))
                {
                    if (result.SelectedSerializer == null)
                    {
                        throw new InvalidOperationException("No seralizer selected.");
                    }
                    _serverConnection.Components["serializer"] = _serializers[result.SelectedSerializer];
                }
                var scene = new Scene(this._serverConnection, this, sceneId, ci.Token, result);
                _scenesDispatcher.AddScene(scene);
                return scene;
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

        internal Task<byte> ConnectToScene(string token, IEnumerable<Route> localRoutes)
        {
            var parameter = new Stormancer.Dto.ConnectToSceneMsg
            {
                Token = token,
                Routes = localRoutes.Select(r => new Stormancer.Dto.RouteDto { }).ToList()

            };
            return this.SendSystemRequest<Stormancer.Dto.ConnectToSceneMsg, Stormancer.Dto.ConnectedToSceneMsg>((byte)MessageIDTypes.ID_CONNECT_TO_SCENE, parameter)
                .Then(result => result.Handle);
        }

        internal Task Disconnect(byte sceneHandle)
        {
            return this.SendSystemRequest<byte, Stormancer.Dto.Empty>((byte)MessageIDTypes.ID_DISCONNECT_FROM_SCENE, sceneHandle)
                .Then(() => this._scenesDispatcher.RemoveScene(sceneHandle));
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

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Client()
        {
            this.Dispose();
        }




        internal IObservable<Packet> SendRequest(IConnection peer, byte scene, ushort route, Action<Stream> writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");

            }
            return _requestProcessor.SendSceneRequest(peer, scene, route, writer);
        }




    }

}
