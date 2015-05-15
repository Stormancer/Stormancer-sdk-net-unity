using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
namespace Stormancer
{
    /// <summary>
    /// Extension methods for ISceneHost
    /// </summary>
    public static class SceneHostExtensions
    {
        /// <summary>
        /// Broadcasts a message to all clients
        /// </summary>
        /// <param name="scene">The scene on which to broadcast</param>
        /// <param name="route">The route of the message</param>
        /// <param name="writer">An action that will write the message</param>
        /// <param name="priority">The message priority level</param>
        /// <param name="reliability">The message reliability level</param>
        public static void Broadcast(this ISceneHost scene, string route, Action<Stream> writer, PacketPriority priority,
            PacketReliability reliability)
        {
            scene.Send(new MatchAllFilter(), route, writer, priority, reliability);

        }

        /// <summary>
        /// Sends a request to a scene without parameters nor response
        /// </summary>
        /// <param name="peer">The target peer</param>
        /// <param name="route">The target route</param>
        /// <param name="priority">The request's priority level</param>
        /// <returns>A task completing when the requests complete on the remote peer</returns>
        public static async Task SendVoidRequest(this IScenePeerClient peer, string route, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            await peer.Rpc(route, s =>
            {

            }, priority).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Sends a request to a scene that doesn't return data.
        /// </summary>
        /// <param name="peer">The target peer</param>
        /// <param name="route">The target route</param>
        /// <param name="parameter">The request parameter</param>
        /// <param name="priority">The request's priority level</param>
        /// <returns>A task completing on request completion</returns>
        public static async Task SendVoidRequest(this IScenePeerClient peer, string route, object parameter, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            await peer.Rpc(route, s =>
            {
                peer.Serializer().Serialize(parameter, s);
            }, priority).FirstOrDefaultAsync();
        }
        /// <summary>
        /// Sends a RPC to an host.
        /// </summary>
        /// <typeparam name="TOut">The type of the data returned by the request.</typeparam>
        /// <param name="peer">The peer to send the request to</param>
        /// <param name="route">The target route.</param>
        /// <param name="parameter">The request parameter</param>
        /// <param name="priority">The request's priority level</param>
        /// <returns>An observable to subscribe to to get responses from the peer.</returns>
        public static IObservable<TOut> SendRequest<TOut>(this IScenePeerClient peer, string route, object parameter, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return peer.Rpc(route, s =>
            {
                peer.Serializer().Serialize(parameter, s);
            }, priority).Select(packet =>
            {
                var value = packet.Serializer().Deserialize<TOut>(packet.Stream);

                return value;
            });
        }

        /// <summary>
        /// Sends a remote procedure call using raw binary data as input and output.
        /// </summary>
        /// <param name="peer">The remote peer </param>
        /// <param name="route">The target route</param>
        /// <param name="writer">A writer method writing</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>An IObservable instance that provides return values for the request.</returns>
        public static IObservable<Packet<IScenePeerClient>> Rpc(this IScenePeerClient peer, string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            var rpcService = peer.Host.GetComponent<Stormancer.Plugins.RpcService>();
            if (rpcService == null)
            {
                throw new NotSupportedException("RPC plugin not available.");
            }
            return rpcService.Rpc(route, peer, writer, priority);
        }


        /// <summary>
        /// Adds a procedure to the scene.
        /// </summary>
        /// <remarks>
        /// Procedures provide an asynchronous request/response pattern on top of scenes using the RPC plugin. 
        /// Procedures can be called by remote peers using the `rpc` method. They support multiple partial responses in a single request.
        /// </remarks>
        /// <param name="scene">The scene to add the procedure to.</param>
        /// <param name="route">The route of the procedure</param>
        /// <param name="handler">A method that implement the procedure logic</param>
        /// <param name="ordered">True if order of the partial responses should be preserved when sent to the client, false otherwise.</param>
        public static void AddProcedure(this ISceneHost scene, string route, Func<Stormancer.Plugins.RequestContext<IScenePeerClient>, Task> handler, bool ordered = true)
        {
            var rpcService = scene.GetComponent<Stormancer.Plugins.RpcService>();
            if(rpcService == null)
            {
                throw new NotSupportedException("RPC plugin not available.");
            }
            rpcService.AddProcedure(route, handler, ordered);
        }

        public static void EnableClientLogs(this IScenePeerClient peer)
        {
            peer.Host.GetComponent<Stormancer.Plugins.ClientLogsService>().EnableClientLogs(peer);
        }

        public static void DisableClientLogs(this IScenePeerClient peer)
        {
            peer.Host.GetComponent<Stormancer.Plugins.ClientLogsService>().DisableClientLogs(peer);
        }
    }
}
