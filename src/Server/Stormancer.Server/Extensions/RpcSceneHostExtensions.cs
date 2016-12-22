using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Stormancer.Plugins;
using Stormancer.Diagnostics;

namespace Stormancer
{
    /// <summary>
    /// RPC extension methods for ISceneHost
    /// </summary>
    public static class RpcSceneHostExtensions
    {

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
        /// <typeparam name="TData">The type of data to send.</typeparam> 
        /// <param name="peer">The target peer.</param>
        /// <param name="route">The target route.</param>
        /// <param name="parameter">The request parameter.</param>
        /// <param name="priority">The request's priority level.</param>
        /// <returns>A task completing on request completion.</returns>
        public static async Task SendVoidRequest<TData>(this IScenePeerClient peer, string route, TData parameter, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            await peer.Rpc(route, s =>
            {
                peer.Serializer().Serialize(parameter, s);
            }, priority).FirstOrDefaultAsync();
        }
        /// <summary>
        /// Sends a RPC to an host.
        /// </summary>
        /// <typeparam name="TData">The type of data to send.</typeparam>
        /// <typeparam name="TResult">The type of the data returned by the request.</typeparam>
        /// <param name="peer">The peer to send the request to.</param>
        /// <param name="route">The target route.</param>
        /// <param name="parameter">The request parameter.</param>
        /// <param name="priority">The request's priority level.</param>
        /// <returns>An observable to subscribe to to get responses from the peer.</returns>
        public static IObservable<TResult> SendRequest<TData, TResult>(this IScenePeerClient peer, string route, TData parameter, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return peer.Rpc(route, s =>
            {
                peer.Serializer().Serialize(parameter, s);
            }, priority).Select(packet =>
            {
                var value = packet.Serializer().Deserialize<TResult>(packet.Stream);

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
            var rpcService = peer.Host.DependencyResolver.Resolve<Stormancer.Plugins.RpcService>();
            if (rpcService == null)
            {
                throw new NotSupportedException("RPC plugin not available.");
            }
            return rpcService.Rpc(route, peer, writer, priority);
        }

        /// <summary>
        /// Sends a remote procedure call with an object as input, expecting any number of answers.
        /// </summary>
        /// <typeparam name="TData">The type of data to send</typeparam>
        /// <typeparam name="TResponse">The expected type of the responses.</typeparam>
        /// <param name="peer">The target peer.</param>
        /// <param name="route">The target route.</param>
        /// <param name="data">The data object to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>An IObservable instance that provides return values for the request.</returns>
        public static IObservable<TResponse> Rpc<TData, TResponse>(this IScenePeerClient peer, string route, TData data, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return peer.Rpc(route, s => peer.Serializer().Serialize(data, s), priority)
                .Select(p => p.ReadObject<TResponse>());
        }

        /// <summary>
        /// Sends a remote procedure call using raw binary data as input, expecting no answer
        /// </summary>
        /// <param name="peer">The target peer. </param>
        /// <param name="route">The target route.</param>
        /// <param name="writer">A writer method writing the data to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>A task representing the remote procedure.</returns>
        public async static Task RpcVoid(this IScenePeerClient peer, string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            await peer.Rpc(route, writer, priority).DefaultIfEmpty();
        }

        /// <summary>
        /// Sends a remote procedure call with an object as input, expecting no answer
        /// </summary>
        /// <typeparam name="TData">The type of data to send</typeparam>
        /// <param name="peer">The target peer.</param>
        /// <param name="route">The target route.</param>
        /// <param name="data">The data object to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>A task representing the remote procedure.</returns>
        public static Task RpcVoid<TData>(this IScenePeerClient peer, string route, TData data, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return peer.RpcVoid(route, s => peer.Serializer().Serialize(data, s), priority);
        }

        /// <summary>
        /// Sends a remote procedure call using raw binary data as input and output, expecting exactly one answer
        /// </summary>
        /// <param name="peer">The target peer. </param>
        /// <param name="route">The target route.</param>
        /// <param name="writer">A writer method writing the data to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>A task representing the remote procedure, whose return value is the raw answer to the remote procedure call.</returns>
        public static async Task<Packet<IScenePeerClient>> RpcTask(this IScenePeerClient peer, string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return await peer.Rpc(route, writer, priority);
        }

        /// <summary>
        /// Sends a remote procedure call with an object as input, expecting exactly one answer
        /// </summary>
        /// <typeparam name="TData">The type of data to send</typeparam>
        /// <typeparam name="TResponse">The expected type of the responses.</typeparam>
        /// <param name="peer">The target peer.</param>
        /// <param name="route">The target route.</param>
        /// <param name="data">The data object to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>A tesk representing the remote procedure, whose return value is the deserialized value of the answer</returns>
        public static async Task<TResponse> RpcTask<TData, TResponse>(this IScenePeerClient peer, string route, TData data, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            var result = await peer.RpcTask(route, s => peer.Serializer().Serialize(data, s), priority);

            return result.ReadObject<TResponse>();
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
           
            scene.Starting.Add(_ =>
            {
                var rpcService = scene.DependencyResolver.Resolve<Stormancer.Plugins.RpcService>();
                scene.DependencyResolver.Resolve<ILogger>().Trace("rpc", $"Adding procedure in starting event'{route}'");
                if (rpcService == null)
                {
                    throw new NotSupportedException("RPC plugin not available.");
                }
                rpcService.AddProcedure(route, handler, ordered);
                return Task.FromResult(true);
            });
           
        }

        /// <summary>
        /// Reads the message from the request.
        /// </summary>
        /// <typeparam name="TData">The expected type of the data.</typeparam>
        /// <param name="context">The request context to read from.</param>
        /// <returns>The deserialized data.</returns>
        /// <remarks>ReadObject will yield you a new object every time you call it. If the request only contains a single object, make sure to call it only once.</remarks>
        public static TData ReadObject<TData>(this RequestContext<IScenePeerClient> context)
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
        public static void SendValue<TData>(this RequestContext<IScenePeerClient> context, TData data, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            context.SendValue(s =>
            {
                context.RemotePeer.Serializer().Serialize(data, s);
            }, priority);
        }
    }
}
