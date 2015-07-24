using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using UniRx;

namespace Stormancer.Networking
{
    internal class WebSocketClientConnection : IConnection
    {
        private class WebSocketConnectionStatistics : IConnectionStatistics
        {
            public float PacketLossRate
            {
                get { return 0; }
            }

            public BPSLimitationType BytesPerSecondLimitationType
            {
                get { return BPSLimitationType.None; }
            }

            public long BytesPerSecondLimit
            {
                get { return 0; }
            }

            public double QueuedBytes
            {
                get { return 0; }
            }

            public double QueuedBytesForPriority(PacketPriority priority)
            {
                return 0;
            }

            public int QueuedPackets
            {
                get { return 0; }
            }

            public int QueuedPacketsForPriority(PacketPriority priority)
            {
                return 0;
            }
        }

        private readonly WebSocket _socket;
        public WebSocketClientConnection(long id, WebSocket socket)
        {
            this._socket = socket;

            this.Id = id;
            this.ConnectionDate = DateTime.UtcNow;
            this.State = ConnectionState.Connected;
        }

        public long Id
        {
            get;
            private set;
        }

        public string IpAddress
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime ConnectionDate
        {
            get;
            internal set;
        }

        private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();
        public Dictionary<string, string> Metadata
        {
            get { return _metadata; }
        }

        private readonly Dictionary<Type, object> _localData = new Dictionary<Type, object>();
        public void RegisterComponent<T>(T component)
        {
            this._localData.Add(typeof(T), component);
        }

        public T GetComponent<T>()
        {
            object result;
            if (_localData.TryGetValue(typeof(T), out result))
            {
                return (T)result;
            }
            else
            {
                return default(T);
            }
        }

        public string Account
        {
            get;
            private set;
        }

        public string Application
        {
            get;
            private set;
        }

        public ConnectionState State
        {
            get;
            private set;
        }

        public void Close()
        {
            this._socket.Close();
        }



        public void SendSystem(byte msgId, Action<Stream> writer)
        {
            SendSystem(msgId, writer, PacketPriority.MEDIUM_PRIORITY);
        }

        public void SendSystem(byte msgId, Action<Stream> writer, PacketPriority priority)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            using (var stream = new MemoryStream())
            {
                stream.WriteByte(msgId);
                writer(stream);

                stream.Seek(0, SeekOrigin.Begin);
                this._socket.Send(stream, (int)stream.Length);
            }
        }

        public void SendToScene(byte sceneHandle, ushort route, Action<System.IO.Stream> writer, PacketPriority priority, PacketReliability reliability)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            using (var stream = new MemoryStream())
            {
                stream.WriteByte(sceneHandle);
                stream.Write(BitConverter.GetBytes(route), 0, 2);
                writer(stream);

                stream.Seek(0, SeekOrigin.Begin);
                this._socket.Send(stream, (int)stream.Length);
            }
        }

        public Action<string> ConnectionClosed
        {
            get;
            set;
        }

        public void SetApplication(string account, string application)
        {
            this.Account = account;
            this.Application = application;
        }

        public int Ping
        {
            get { return 0; }
        }

        public IConnectionStatistics GetConnectionStatistics()
        {
            return new WebSocketConnectionStatistics();
        }

        internal void RaiseConnectionClosed(string reason)
        {
            var action = this.ConnectionClosed;

            if (action != null)
            {
                action(reason);
            }
        }

    }
}
