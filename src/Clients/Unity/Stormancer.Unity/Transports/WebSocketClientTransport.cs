using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using Stormancer.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using System.IO;
using WebSocketSharp;
using UniRx;
using CancellationToken = System.Threading.CancellationToken;
using LogLevel = Stormancer.Diagnostics.LogLevel;

namespace Stormancer.Networking
{
    internal class WebSocketClientTransport : ITransport
    {
        private const string LOGCATEGORY = "transports.WebSocket";

        private WebSocket _socket;
        private WebSocketClientConnection _connection;

        private string _type;
        private IConnectionManager _connectionManager;

        private readonly Stormancer.Diagnostics.ILogger _logger;
        public WebSocketClientTransport(Stormancer.Diagnostics.ILogger logger)
        {
            this._logger = logger;
        }
        public Task Start(string name, IConnectionManager handler, CancellationToken token, ushort? port, ushort maxConnections)
        {
            this._type = name;

            this._connectionManager = handler;

            try
            {
                IsRunning = true;

                if (port != null)
                {
                    throw new NotImplementedException();
                }

                token.Register(this.Stop);
            }
            finally
            {
            }

            return TaskHelper.FromResult(Unit.Default);
        }

        public bool IsRunning
        {
            get;
            private set;
        }

        public Task<IConnection> Connect(string endpoint)
        {
            if (this._socket == null && !this._connecting)
            {

                this._connecting = true;
                try
                {
                    var webSocket = new WebSocket("ws://" + endpoint + "/");
                    //try 
                    //{
                    this.ConnectSocket(webSocket);
                    this._socket = webSocket;
                    //}
                    //catch(Exception ex)
                    //{
                    //  webSocket = new WebSocket("ws://" + endpoint);
                    //}

                    //if (this._socket == null)
                    //{
                    //    this.ConnectSocket(webSocket);
                    //}

                    var connection = this.CreateNewConnection(this._socket);

                    this._connectionManager.NewConnection(connection);

                    var action = this.ConnectionOpened;

                    if (action != null)
                    {
                        action(connection);
                    }

                    this._connection = connection;

                    return TaskHelper.FromResult<IConnection>(connection);
                }
                finally
                {
                    this._connecting = false;
                }
            }
            else
            {
                throw new InvalidOperationException("This transport is already connected.");
            }
        }

        private void ConnectSocket(WebSocket webSocket)
        {
            try
            {
                webSocket.OnOpen += (sender, args) => this.OnOpen(webSocket);
                webSocket.OnMessage += (sender, args) => this.OnMessage(args.RawData);
                webSocket.OnClose += (sender, args) => this.OnClose(args.WasClean);

                webSocket.Connect();
            }
            catch (Exception)
            {
                webSocket.Dispose();
                throw;
            }
        }


        public Action<Packet> PacketReceived
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

        public string Name
        {
            get { return "websocket"; }
        }

        public long? Id
        {
            get;
            internal set;
        }

        private bool _connecting = false;

        private void OnOpen(WebSocket socket)
        {
        }

        private WebSocketClientConnection CreateNewConnection(WebSocket socket)
        {
            var cid = _connectionManager.GenerateNewConnectionId();
            return new WebSocketClientConnection(cid, socket);
        }

        private void OnClose(bool clean)
        {
            var reason = clean ? "CLIENT_DISCONNECTED" : "CONNECTION_LOST";

            var connection = this._connection;
            if (connection != null)
            {
                var logData = new { clean };
                this._logger.Log(LogLevel.Trace, LOGCATEGORY, string.Format("{0} disconnected.", connection.Id), logData);

                this._connectionManager.CloseConnection(connection, reason);

                connection.RaiseConnectionClosed(reason);

                var action = this.ConnectionClosed;

                if (action != null)
                {
                    action(connection);
                }
            }

        }

        private void OnMessage(byte[] data)
        {
            var packet = new Packet(this._connection, new MemoryStream(data));

            var logData = new { messageType = data[0] };
            this._logger.Log(LogLevel.Trace, LOGCATEGORY, string.Format("message with id {0} arrived", data[0]), logData);

            if (data[0] == (byte)MessageIDTypes.ID_CONNECTION_RESULT)
            {
                this.OnConnectionIdReceived(BitConverter.ToInt64(data, 1));
            }
            else
            {
                this.PacketReceived(packet);
            }
        }

        private void OnConnectionIdReceived(long id)
        {
            Id = id;
        }


        private void Stop()
        {
            this.IsRunning = false;
            if (this._socket != null)
            {
                this._socket.Dispose();
                this._socket = null;
            }
        }
    }
}
