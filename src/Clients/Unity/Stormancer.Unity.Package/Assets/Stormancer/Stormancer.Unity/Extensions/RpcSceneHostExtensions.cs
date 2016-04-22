using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// RPC extension methods for ISceneHost
    /// </summary>
    public static class RpcSceneHostExtensions
    {
        /// <summary>
        /// Reads the message from the request.
        /// </summary>
        /// <typeparam name="TData">The expected type of the data.</typeparam>
        /// <param name="context">The request context to read from.</param>
        /// <returns>The deserialized data.</returns>
        /// <remarks>ReadObject will yield you a new object every time you call it. If the request only contains a single object, make sure to call it only once.</remarks>
        public static TData ReadObject<TData>(this RequestContext<IScenePeer> context)
        {
            var serializer = context.RemotePeer.Serializer();
            return serializer.Deserialize<TData>(context.InputStream);
        }

        /// <summary>
        /// Sends an object as a response to a request.
        /// </summary>
        /// <typeparam name="TData">The type of object to send as a response.</typeparam>
        /// <param name="context">The request context to respond to.</param>
        /// <param name="data">The data to send as a response.</param>
        /// <param name="priority">The priority of the response.</param>
        public static void SendValue<TData>(this RequestContext<IScenePeer> context, TData data, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            context.SendValue(s =>
            {
                context.RemotePeer.Serializer().Serialize(data, s);
            }, priority);
        }
    }
}
