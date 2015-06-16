using MsgPack.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
#if StormancerClient
#else
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR;
#endif
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using Stormancer.Client45.Infrastructure;
using Stormancer.Client45;
using Stormancer.Networking;
using Stormancer.Core;
using Stormancer.Cluster.Application;
using Stormancer.Dto;
using Stormancer.Plugins;
using Stormancer.Diagnostics;
using System.Diagnostics;

namespace Stormancer
{
    /// <summary>
    /// Stormancer client class
    /// </summary>
    /// <example>
    /// 
    /// </example>
    public class Client : IDisposable
    {
        private class ConnectionHandler : IConnectionManager
        {
            private long _current = 0;
            public long GenerateNewConnectionId()
            {
                return Interlocked.Increment(ref _current) - 1;
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
            public int ConnectionCount { get { return 0; } }
        }
        private readonly ApiClient _apiClient;
        private readonly string _accountId;
        private readonly string _applicationName;
        private readonly int _pingInterval = 5000;

        private readonly PluginBuildContext _pluginCtx = new PluginBuildContext();
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
        private readonly IScheduler _scheduler;

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
            this._scheduler = configuration.Scheduler;
            this._logger = configuration.Logger;
            this._accountId = configuration.Account;
            this._applicationName = configuration.Application;
            _apiClient = new ApiClient(configuration, _tokenHandler);
            this._transport = configuration.TransportFactory(new Dictionary<string, object> { { "ILogger", this._logger }, { "IScheduler", this._scheduler } });
            this._dispatcher = configuration.Dispatcher;
            _requestProcessor = new Stormancer.Networking.Processors.RequestProcessor(_logger, Enumerable.Empty<IRequestModule>());

            _scenesDispatcher = new Processors.SceneDispatcher();
            this._dispatcher.AddPRocessor(_requestProcessor);
            this._dispatcher.AddPRocessor(_scenesDispatcher);
            this._metadata = configuration._metadata;

            foreach (var serializer in configuration.Serializers)
            {
                this._serializers.Add(serializer.Name, serializer);
            }

            this._metadata.Add("serializers", string.Join(",", this._serializers.Keys));
            this._metadata.Add("transport", _transport.Name);
            this._metadata.Add("version", "1.0.0a");
            this._metadata.Add("platform", "NET45");
            this._metadata.Add("protocol", "2");

            this._maxPeers = configuration.MaxPeers;

            foreach (var plugin in configuration.Plugins)
            {
                plugin.Build(_pluginCtx);
            }
            if (_pluginCtx.ClientCreated != null)
                _pluginCtx.ClientCreated(this);
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
                _initialized = true;

                _transport.PacketReceived += _transport_PacketReceived;

                _watch.Start();

            }

        }

        void _transport_PacketReceived(Stormancer.Core.Packet obj)
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
        public async Task<Scene> GetPublicScene<T>(string sceneId, T userData)
        {
            var ci = await _apiClient.GetSceneEndpoint(this._accountId, this._applicationName, sceneId, userData);


            return await GetScene(sceneId, ci);
        }

        private async Task<U> SendSystemRequest<T, U>(byte id, T parameter)
        {
            var packet = await _requestProcessor.SendSystemRequest(_serverConnection, id, s =>
            {
                _systemSerializer.Serialize(parameter, s);
            });
            var result = _systemSerializer.Deserialize<U>(packet.Stream);

            return result;
        }

        private Task UpdateServerMetadata()
        {
            return _requestProcessor.SendSystemRequest(_serverConnection, (byte)SystemRequestIDTypes.ID_SET_METADATA, s =>
            {
                _systemSerializer.Serialize(_serverConnection.Metadata, s);
            });
        }
        private async Task SyncClockImpl()
        {
            try
            {
                long tStart = _watch.ElapsedMilliseconds;
                var response = await _requestProcessor.SendSystemRequest(_serverConnection, (byte)SystemRequestIDTypes.ID_PING, s =>
                {
                    s.Write(BitConverter.GetBytes(tStart), 0, 8);
                }, PacketPriority.IMMEDIATE_PRIORITY);
                ulong tRef;
                long tEnd;
                using (var reader = new BinaryReader(response.Stream))
                {
                    tRef = reader.ReadUInt64();
                    tEnd = reader.ReadInt64();
                }
                LastPing = tEnd - tStart;
                _offset = (long)tRef - (tStart + tEnd) / 2;
            }
            catch (Exception)
            {
                _logger.Error("ping", "failed to ping server.");
            };
        }
        private IDisposable _syncClockTaskDisposable;
        private void StartSyncClock()
        {

            _syncClockTaskDisposable = _scheduler.SchedulePeriodic(_pingInterval, () => { var _ = SyncClockImpl(); });
        }

        private async Task<Scene> GetScene(string sceneId, SceneEndpoint ci)
        {
            if (_serverConnection == null)
            {
                if (!_transport.IsRunning)
                {
                    cts = new CancellationTokenSource();
                    _transport.Start("client", new ConnectionHandler(), cts.Token, null, (ushort)(_maxPeers + 1));
                    StartSyncClock();
                }
                _serverConnection = await _transport.Connect(ci.TokenData.Endpoints[_transport.Name]);

                foreach (var kvp in _metadata)
                {
                    _serverConnection.Metadata[kvp.Key] = kvp.Value;
                }
                await UpdateServerMetadata();

            }
            var parameter = new Stormancer.Dto.SceneInfosRequestDto { Metadata = _serverConnection.Metadata, Token = ci.Token };

            var result = await SendSystemRequest<Stormancer.Dto.SceneInfosRequestDto, Stormancer.Dto.SceneInfosDto>((byte)SystemRequestIDTypes.ID_GET_SCENE_INFOS, parameter);

            if (_serverConnection.GetComponent<ISerializer>() == null)
            {
                if (result.SelectedSerializer == null)
                {
                    throw new InvalidOperationException("No seralizer selected.");
                }
                _serverConnection.RegisterComponent(_serializers[result.SelectedSerializer]);
                _serverConnection.Metadata.Add("serializer", result.SelectedSerializer);
            }
            await UpdateServerMetadata();
            var scene = new Scene(this._serverConnection, this, sceneId, ci.Token, result);

            if (_pluginCtx.SceneCreated != null)
            {
                _pluginCtx.SceneCreated(scene);
            }
            return scene;


        }


        /// <summary>
        /// Returns a private scene (requires a token obtained from strong authentication with the Stormancer API.
        /// </summary>
        /// <remarks>
        /// The connection happens when the method `Connect` is called on the scene object. Note that when you call GetScene, 
        /// a connection token is requested from the Stormancer API.this token is only valid for a few minutes: Don't get scenes
        /// a long time before connecting to them.
        /// </remarks>
        /// <param name="token">The token securing the connection.</param>
        /// <returns>A task returning the scene object on completion.</returns>
        public async Task<Scene> GetScene(string token)
        {
            var ci = _tokenHandler.DecodeToken(token);
            return await GetScene(ci.TokenData.SceneId, ci);
        }

        internal async Task ConnectToScene(Scene scene, string token, IEnumerable<Route> localRoutes)
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
            var result = await this.SendSystemRequest<Stormancer.Dto.ConnectToSceneMsg, Stormancer.Dto.ConnectionResult>((byte)SystemRequestIDTypes.ID_CONNECT_TO_SCENE, parameter);
            scene.CompleteConnectionInitialization(result);
            _scenesDispatcher.AddScene(scene);

            //Send ready message. It will fires the Connected event on the server. If not sent, the connection to the scene will timeout.
            //await _requestProcessor.SendSystemRequest(_serverConnection, (byte)SystemRequestIDTypes.ID_SCENE_READY, s =>
            //{
            //    s.WriteByte(scene.Handle);
            //});

            if (_pluginCtx.SceneConnected != null)
            {
                _pluginCtx.SceneConnected(scene);
            }
        }


        internal async Task Disconnect(Scene scene, byte sceneHandle)
        {
            await this.SendSystemRequest<byte, Stormancer.Dto.Empty>((byte)SystemRequestIDTypes.ID_DISCONNECT_FROM_SCENE, sceneHandle);
            this._scenesDispatcher.RemoveScene(sceneHandle);
            if (_pluginCtx.SceneDisconnected != null)
                _pluginCtx.SceneDisconnected(scene);
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
                if (_syncClockTaskDisposable != null)
                {
                    _syncClockTaskDisposable.Dispose();
                }
                Disconnect();

            }

        }


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
