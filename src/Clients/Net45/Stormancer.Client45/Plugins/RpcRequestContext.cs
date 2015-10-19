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
    /// <summary>
    /// Context object that provides informations about a running request
    /// </summary>
    /// <typeparam name="T">Type of the remote peer</typeparam>
    /// <remarks>
    /// The context is provided to your handler when you create a request route.
    /// </remarks>
    public class RequestContext<T> where T : IScenePeer
    {
        private Scene _scene;
        private ushort id;
        private bool _ordered;
        private T _peer;
        private byte _msgSent;

        /// <summary>
        /// The peer that sent the request
        /// </summary>
        public T RemotePeer
        {
            get
            {
                return _peer;
            }
        }

        /// <summary>
        /// Stream instance containing the binary data sent with the request.
        /// </summary>
        public Stream InputStream
        {
            get;
            private set;
        }

        internal RequestContext(T peer, Scene scene, ushort id, bool ordered, Stream inputStream, CancellationToken token)
        {
            this._scene = scene;
            this.id = id;
            this._ordered = ordered;
            this._peer = peer;
            this.InputStream = inputStream;
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

        /// <summary>
        /// Sends a partial response to the client through the request channel
        /// </summary>
        /// <param name="writer">An action that writes the response message</param>
        /// <param name="priority">A priority level to use to send the message</param>
        /// <remarks>
        /// All request responses are sent using the RELIABLE_ORDERED reliability level
        /// On the client, code that subscribed to the observable returned by the request send method will receive the messages 
        /// sent through the `SendValue` method.
        /// </remarks>
        public void SendValue(Action<Stream> writer, PacketPriority priority)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }
            _scene.SendPacket(RpcClientPlugin.NextRouteName, s =>
            {
                WriteRequestId(s);
                writer(s);
            }, priority, this._ordered ? PacketReliability.RELIABLE_ORDERED : PacketReliability.RELIABLE);
            _msgSent = 1;
        }


        internal void SendError(string errorMsg)
        {
            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }
            this._scene.SendPacket(RpcClientPlugin.ErrorRouteName, s =>
            {
                WriteRequestId(s);
                _peer.Serializer().Serialize(errorMsg, s);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_ORDERED);
        }

        internal void SendCompleted()
        {
            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }
            this._scene.SendPacket(RpcClientPlugin.CompletedRouteName, s =>
            {
                s.WriteByte(_msgSent);
                WriteRequestId(s);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_ORDERED);
        }
    }
}
