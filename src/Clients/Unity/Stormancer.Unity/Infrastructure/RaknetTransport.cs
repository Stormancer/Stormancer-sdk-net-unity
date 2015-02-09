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

namespace Stormancer.Networking
{
    public class RaknetTransport : ITransport
    {
        private IConnectionManager _handler;
        private RakPeerInterface _peer;
        private ILogger logger;
        private string _type;
        private readonly ConcurrentDictionary<ulong, RakNetConnection> _connections = new ConcurrentDictionary<ulong, RakNetConnection>();

        public RaknetTransport(ILogger logger)
        {
            this.logger = logger;
        }
        public Task Start(string type, IConnectionManager handler, CancellationToken token, ushort? serverPort, ushort maxConnections)
        {
            if (handler == null && serverPort.HasValue)
            {
                throw new ArgumentNullException("handler");
            }
            _type = type;

            var tcs = new TaskCompletionSource<bool>();
            _handler = handler;
            Task.Factory.StartNew(() => Run(token, serverPort, maxConnections, tcs));
            return tcs.Task;
        }

        private const int connectionTimeout = 5000;

        private void Run(CancellationToken token, ushort? serverPort, ushort maxConnections, TaskCompletionSource<bool> startupTcs)
        {
            IsRunning = true;
            logger.Info("starting raknet transport " + _type);
            var server = RakPeerInterface.GetInstance();

            var socketDescriptor = serverPort.HasValue ? new SocketDescriptor(serverPort.Value, null) : new SocketDescriptor();
            var startupResult = server.Startup(maxConnections, socketDescriptor, 1);
            if(startupResult!= StartupResult.RAKNET_STARTED)
            {
                throw new InvalidOperationException("Couldn't start raknet peer :" + startupResult);
            }
            server.SetMaximumIncomingConnections(maxConnections);

            _peer = server;
            startupTcs.SetResult(true);
            logger.Info("Raknet transport started " + _type);
            while (!token.IsCancellationRequested)
            {
                for (var packet = server.Receive(); packet != null; packet = server.Receive())
                {



                    switch (packet.data[0])
                    {
                        case (byte)DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED:
                            TaskCompletionSource<IConnection> tcs;
                            if (_pendingConnections.TryGetValue(packet.systemAddress.ToString(), out tcs))
                            {
                                var c = CreateNewConnection(packet.guid, server);
                                tcs.SetResult(c);
                            }
                            logger.Debug("Connection request to {0} accepted.", packet.systemAddress.ToString());
                            OnConnection(packet, server);
                            break;
                        case (byte)DefaultMessageIDTypes.ID_NEW_INCOMING_CONNECTION:
                            logger.Trace("Incoming connection from {0}.", packet.systemAddress.ToString());
                            OnConnection(packet, server);
                            break;

                        case (byte)DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                            logger.Trace("{0} disconnected.", packet.systemAddress.ToString());
                            OnDisconnection(packet, server,"CLIENT_DISCONNECTED");
                            break;
                        case (byte)DefaultMessageIDTypes.ID_CONNECTION_LOST:
                            logger.Trace("{0} lost the connection.", packet.systemAddress.ToString());
                            OnDisconnection(packet, server, "CONNECTION_LOST");

                            break;
                        case (byte)MessageIDTypes.ID_CONNECTION_RESULT:
                            OnConnectionIdReceived(BitConverter.ToInt64(packet.data, 1));
                            break;

                        case (byte)DefaultMessageIDTypes.ID_CONNECTION_ATTEMPT_FAILED:
                            if (_pendingConnections.TryGetValue(packet.systemAddress.ToString(), out tcs))
                            {
                                tcs.SetException(new InvalidOperationException("Connection attempt failed."));
                            }
                            break;

                        default:
                            OnMessageReceived(packet);
                            break;
                    }
                }
                Thread.Sleep(5);
            }
            server.Shutdown(1000);
            IsRunning = false;
            logger.Info("Stopped raknet server.");
        }

        private void OnConnectionIdReceived(long p)
        {
            Id = p;
        }

        #region message handling

        private void OnConnection(RakNet.Packet packet, RakPeerInterface server)
        {
            logger.Trace("{0} connected", packet.systemAddress);

            var c = CreateNewConnection(packet.guid, server);
            server.DeallocatePacket(packet);
            _handler.NewConnection(c);
            var action = ConnectionOpened;
            if (action != null)
            {
                action(c);
            }

            c.SendSystem((byte)MessageIDTypes.ID_CONNECTION_RESULT, s => s.Write(BitConverter.GetBytes(c.Id), 0, 8));
        }


        private void OnDisconnection(RakNet.Packet packet, RakPeerInterface server,string reason)
        {
            logger.Trace("{0} disconnected", packet.systemAddress);
            var c = RemoveConnection(packet.guid);
            server.DeallocatePacket(packet);

            _handler.CloseConnection(c,reason);

            var action = ConnectionClosed;
            if (action != null)
            {
                action(c);
            }
            c.ConnectionClosed(reason);
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
            logger.Trace("message with id {0} arrived", packet.data[0]);

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
            _peer.Connect(host, port, null, 0);

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
    }



}
