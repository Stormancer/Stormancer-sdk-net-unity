using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Stormancer.Plugins
{

    public class RequestContext<T> where T : IScenePeer
    {
        private Scene _scene;
        private ushort id;
        private bool _ordered;
        private T _peer;

        public T RemotePeer
        {
            get
            {
                return _peer;
            }
        }

        public Stream InputStream
        {
            get;
            private set;
        }

        internal RequestContext(T peer, Scene scene, ushort id, bool ordered, Stream stream, CancellationToken token)
        {
            // TODO: Complete member initialization
            this._scene = scene;
            this.id = id;
            this._ordered = ordered;
            this._peer = peer;
            InputStream = stream;
            CancellationToken = token;
        }

        /// <summary>
        /// A Token that gets cancelled if the client cancels the RPC.
        /// </summary>
        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        private void WriteRequestId(Stream s)
        {
            s.Write(BitConverter.GetBytes(id), 0, 2);
        }
        public void SendValue(Action<Stream> writer, PacketPriority priority)
        {
            _scene.SendPacket(RpcClientPlugin.NextRouteName, s =>
            {
                WriteRequestId(s);
                writer(s);
            }, priority, this._ordered ? PacketReliability.RELIABLE_ORDERED : PacketReliability.RELIABLE);
        }


        internal void SendError(string errorMsg)
        {
            this._scene.SendPacket(RpcClientPlugin.ErrorRouteName, s =>
            {
                WriteRequestId(s);
                _peer.Serializer().Serialize(errorMsg, s);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
        }

        internal void SendCompleted()
        {
            this._scene.SendPacket(RpcClientPlugin.CompletedRouteName, s =>
            {
                WriteRequestId(s);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
        }
    }
}
