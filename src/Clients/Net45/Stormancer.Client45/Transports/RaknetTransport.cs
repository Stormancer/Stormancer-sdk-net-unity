using RakNet;
using Stormancer.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stormancer.Diagnostics;
using Stormancer.Networking.Messages;

namespace Stormancer.Networking
{
    internal class RakNetTransport : ITransport
    {
        private IConnectionManager _handler;
        private RakPeerInterface _peer;
        private ILogger _logger;
        private string _type;
        private readonly ConcurrentDictionary<ulong, RakNetConnection> _connections = new ConcurrentDictionary<ulong, RakNetConnection>();
        private IDisposable _scheduledTransportLoop;
        private IScheduler _scheduler;

        public RakNetTransport(ILogger logger, IScheduler scheduler)
        {
            this._logger = logger;
            this._scheduler = scheduler;
        }

        public void Start(string type, IConnectionManager handler, CancellationToken token, ushort? serverPort, ushort maxConnections)
        {
            if (handler == null && serverPort.HasValue)
            {
                throw new ArgumentNullException("handler");
            }
            _type = type;


            _handler = handler;
            Initialize(serverPort, maxConnections);
            _scheduledTransportLoop = _scheduler.SchedulePeriodic(15, Run);
        }

        private const int connectionTimeout = 5000;

        private void Initialize(ushort? serverPort, ushort maxConnections)
        {
            try
            {
                IsRunning = true;
                _logger.Info("transports.raknet", "starting raknet transport " + _type);

                _peer = RakPeerInterface.GetInstance();
                var socketDescriptor = serverPort.HasValue ? new SocketDescriptor(serverPort.Value, null) : new SocketDescriptor();
                var startupResult = _peer.Startup(maxConnections, socketDescriptor, 1);
                if (startupResult != StartupResult.RAKNET_STARTED)
                {
                    throw new InvalidOperationException("Couldn't start raknet peer :" + startupResult);
                }
                _peer.SetMaximumIncomingConnections(maxConnections);
            }
            catch (Exception e)
            {

                throw new InvalidOperationException("Failed to initialize Raknet", e);
            }
        }

        public void Run()
        {
            try
            {
                _logger.Info("transports.raknet", "Raknet transport started " + _type);

                for (var packet = _peer.Receive(); packet != null; packet = _peer.Receive())
                {
                    try
                    {
                        switch (packet.data[0])
                        {
                            case (byte)DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED:
                                TaskCompletionSource<IConnection> tcs;
                                if (_pendingConnections.TryGetValue(packet.systemAddress.ToString(), out tcs))
                                {
                                    _logger.Debug("Connection request to {0} accepted.", packet.systemAddress.ToString());
                                    OnConnection(packet, _peer);

                                    RakNetConnection c;
                                    if (_connections.TryGetValue(packet.guid.g, out c))
                                    {
                                        tcs.SetResult(c);
                                    }
                                    else
                                    {
                                        _logger.Error("Can't get the peer connection.", packet.systemAddress.ToString());
                                    }
                                }
                                else
                                {
                                    _logger.Error("Can't get the pending connection TCS.", packet.systemAddress.ToString());
                                }
                                break;
                            case (byte)DefaultMessageIDTypes.ID_NEW_INCOMING_CONNECTION:
                                _logger.Trace("Incoming connection from {0}.", packet.systemAddress.ToString());
                                OnConnection(packet, _peer);
                                break;
                            case (byte)DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                                _logger.Trace("{0} disconnected.", packet.systemAddress.ToString());
                                OnDisconnection(packet, _peer, "CLIENT_DISCONNECTED");
                                break;
                            case (byte)DefaultMessageIDTypes.ID_CONNECTION_LOST:
                                _logger.Trace("{0} lost the connection.", packet.systemAddress.ToString());
                                OnDisconnection(packet, _peer, "CONNECTION_LOST");
                                break;
                            case (byte)DefaultMessageIDTypes.ID_CONNECTION_ATTEMPT_FAILED:
                                if (_pendingConnections.TryGetValue(packet.systemAddress.ToString(), out tcs))
                                {
                                    tcs.SetException(new InvalidOperationException("Connection attempt failed."));
                                }
                                break;
                            case (byte)MessageIDTypes.ID_CONNECTION_RESULT:
                                OnConnectionIdReceived(BitConverter.ToInt64(packet.data, 1));
                                break;
                            default:
                                OnMessageReceived(packet);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, "transports.raknet", "An error occured while handling a message", new ExceptionMsg(ex));
                    }
                }
            }
            catch (Exception ex)
            {

                _logger.Error("transports.raknet", "An error occured while running the transport : {0}", ex);

            }
        }

        private void OnConnectionIdReceived(long p)
        {
            Id = p;
        }

        #region message handling

        private void OnConnection(RakNet.Packet packet, RakPeerInterface server)
        {
            _logger.Trace("transports.raknet", "{0} connected", packet.systemAddress);

            var c = CreateNewConnection(packet.guid, _peer);
            server.DeallocatePacket(packet);
            _handler.NewConnection(c);

            var action = ConnectionOpened;
            if (action != null)
            {
                action(c);
            }

            c.SendSystem((byte)MessageIDTypes.ID_CONNECTION_RESULT, s => s.Write(BitConverter.GetBytes(c.Id), 0, 8));
        }

        private void OnDisconnection(RakNet.Packet packet, RakPeerInterface server, string reason)
        {
            _logger.Trace("transports.raknet", "{0} disconnected", packet.systemAddress);

            var c = RemoveConnection(packet.guid);
            server.DeallocatePacket(packet);
            _handler.CloseConnection(c, reason);
            c.RaiseConnectionClosed(reason);

            var action = ConnectionClosed;
            if (action != null)
            {
                action(c);
            }
        }

        private void OnMessageReceived(RakNet.Packet packet)
        {
            var connection = GetConnection(packet.guid);
            var buffer = new byte[packet.data.Length];
            packet.data.CopyTo(buffer, 0);
            _peer.DeallocatePacket(packet);
            var p = new Stormancer.Core.Packet(
                               connection,
                               new MemoryStream(buffer));
            _logger.Trace("transports.raknet", "message with id {0} arrived", buffer[0]);

            this.PacketReceived(p);
        }
        #endregion

        #region manage connections
        private RakNetConnection GetConnection(RakNetGUID guid)
        {
            return _connections[guid.g];
        }

        private RakNetConnection CreateNewConnection(RakNetGUID raknetGuid, RakPeerInterface peer)
        {
            var cid = _handler.GenerateNewConnectionId();
            var c = new RakNetConnection(raknetGuid, cid, peer, OnRequestClose);
            _connections.TryAdd(raknetGuid.g, c);
            return c;
        }

        private RakNetConnection RemoveConnection(RakNetGUID guid)
        {
            RakNetConnection connection;
            _connections.TryRemove(guid.g, out connection);
            return connection;
        }

        private void OnRequestClose(RakNetConnection c)
        {
            _peer.CloseConnection(c.Guid, true);
        }

        #endregion
        
        public Action<Stormancer.Core.Packet> PacketReceived
        {
            get;
            set;
        }

        public Action<IConnection> ConnectionOpened
        {
            get;
            set;
        }

        public Action<IConnection> ConnectionClosed
        {
            get;
            set;
        }
        
        public Task<IConnection> Connect(string endpoint)
        {
            if (_peer == null || !_peer.IsActive())
            {

                throw new InvalidOperationException("Transport not started. Call Start before connect.");
            }
            var infos = endpoint.Split(':');
            var host = infos[0];
            var port = ushort.Parse(infos[1]);
            var result = _peer.Connect(host, port, null, 0);

            var address = new SystemAddress(host, port);

            var tcs = new TaskCompletionSource<IConnection>();

            _pendingConnections.TryAdd(address.ToString(), tcs);

            return tcs.Task;
        }

        private ConcurrentDictionary<string, TaskCompletionSource<IConnection>> _pendingConnections = new ConcurrentDictionary<string, TaskCompletionSource<IConnection>>();

        public string Name
        {
            get { return "raknet"; }
        }
        
        public bool IsRunning
        {
            get;
            private set;
        }

        public long? Id { get; private set; }
        
        public void Dispose()
        {
            if (_scheduledTransportLoop != null)
            {
                _scheduledTransportLoop.Dispose();
                if (_peer != null)
                {
                    if (_peer.IsActive())
                    {
                        _peer.Shutdown(1000);
                    }
                    RakPeerInterface.DestroyInstance(_peer);
                }
                IsRunning = false;
                _logger.Info("transports.raknet", "Stopped raknet server.");
            }
        }
    }
}
