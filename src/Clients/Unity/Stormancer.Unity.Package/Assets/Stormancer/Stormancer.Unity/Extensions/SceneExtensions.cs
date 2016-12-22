using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniRx;
namespace Stormancer
{
    /// <summary>
    /// Extensions for the Scene class.
    /// </summary>
    public static class SceneExtensions
    {
        /// <summary>
        /// Listen to messages on the specified route, deserialize them and execute the given handler for eah of them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene">The remote scene proxy on which the route messages will be listened.</param>
        /// <param name="route">The route to listen.</param>
        /// <param name="handler">The handler to execute for each message on the route.</param>
        /// <returns>An IDisposable object you can use to unregister the handler.</returns>
        public static IDisposable AddRoute<T>(this Scene scene, string route, Action<T> handler)
        {
            return scene.OnMessage<T>(route).Subscribe(handler);
        }

        /// <summary>
        /// Listen to messages on the specified route, deserialize them and execute the given handler for eah of them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene">The remote scene proxy on which the route messages will be listened.</param>
        /// <param name="route">The route to listen.</param>
        /// <param name="handler">The handler to execute for each message on the route.</param>
        /// <returns>An IDisposable object you can use to unregister the handler.</returns>
        /// <remarks>RegisterRoute is an alias to the AddRoute method.</remarks>
        public static IDisposable RegisterRoute<T>(this Scene scene, string route, Action<T> handler)
        {
            return scene.AddRoute(route,handler);
        }

        /// <summary>
        /// Listen to messages on the specified route, and output instances of T using the scene serializer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        public static IObservable<T> OnMessage<T>(this Scene scene, string route)
        {


            return scene.OnMessage(route).Select(packet =>
            {
                var value = packet.Serializer().Deserialize<T>(packet.Stream);
                
                return value;
            });
        }

       
        ///// <summary>
        ///// Sends a request to the remote scene on the route, serializing the data with the scene serializer.
        ///// </summary>
        ///// <typeparam name="T">Type of the input request data.</typeparam>
        ///// <typeparam name="U">Type of the output request data.</typeparam>
        ///// <param name="scene">The remote scene proxy to which the request will be sent.</param>
        ///// <param name="route">A String containing the name of the route to which the request should be sent.</param>
        ///// <param name="data">The Input request data.</param>
        ///// <returns>An observable outputting the request responses.</returns>
        //public static IObservable<U> SendRequest<T, U>(this Scene scene, string route, T data)
        //{
        //    return scene.SendRequest(route, s =>
        //    {
        //        scene.Host.Serializer().Serialize(data, s);
        //    }).Select(packet =>
        //    {
        //        var value = scene.Host.Serializer().Deserialize<U>(packet.Stream);
               
        //        return value;
        //    });
        //}

        //public static Task SendVoidRequest<T>(this Scene scene, string route, T data)
        //{
        //    var tcs = new TaskCompletionSource<Unit>();
        //    scene.SendRequest(route, s =>
        //    {
        //        scene.Host.Serializer().Serialize(data, s);
        //    }).Subscribe(p => { }, () => tcs.SetResult(Unit.Default));

        //    return tcs.Task;
        //}

        //public static Task SendVoidRequest(this Scene scene, string route)
        //{
        //    var tcs = new TaskCompletionSource<Unit>();
        //    scene.SendRequest(route, s =>
        //    {
        //    }).Subscribe(p => { }, () => tcs.SetResult(Unit.Default));

        //    return tcs.Task;
        //}
        //public static IObservable<T> SendRequest<T>(this Scene scene, string route)
        //{
        //    return scene.SendRequest(route, s =>
        //    {
        //    }).Select(packet =>
        //    {
        //        var value = packet.Serializer().Deserialize<T>(packet.Stream);
                
        //        return value;
        //    });
        //}

        public static void Send<T>(this Scene scene, string route, T data)
        {
            scene.SendPacket(route, s =>
            {
                scene.HostConnection.Serializer().Serialize(data, s);
            });
        }

        /// <summary>
        /// Sends a remote procedure call using raw binary data as input and output.
        /// </summary>
        /// <param name="scene">The target scene. </param>
        /// <param name="route">The target route</param>
        /// <param name="writer">A writer method writing</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>An IObservable instance that provides return values for the request.</returns>
        public static IObservable<Packet<IScenePeer>> Rpc(this Scene scene, string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            var rpcService = scene.DependencyResolver.Resolve<Stormancer.Plugins.RpcClientPlugin.RpcService>();
            if (rpcService == null)
            {
                throw new NotSupportedException("RPC plugin not available.");
            }
            
            return rpcService.Rpc(route, writer, priority);
        }

        /// <summary>
        /// Sends a remote procedure call with an object as input, expecting any number of answers.
        /// </summary>
        /// <typeparam name="TData">The type of data to send</typeparam>
        /// <typeparam name="TResponse">The expected type of the responses.</typeparam>
        /// <param name="scene">The target scene.</param>
        /// <param name="route">The target route.</param>
        /// <param name="data">The data object to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>An IObservable instance that provides return values for the request.</returns>
        public static IObservable<TResponse> Rpc<TData, TResponse>(this Scene scene, string route, TData data, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return scene.Rpc(route, s => scene.Host.Serializer().Serialize(data, s), priority)
                .Select(p => p.ReadObject<TResponse>());
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
        public static void AddProcedure(this Scene scene, string route, Func<Stormancer.Plugins.RequestContext<IScenePeer>, Task> handler, bool ordered = true)
        {
            var rpcService = scene.DependencyResolver.Resolve<Stormancer.Plugins.RpcClientPlugin.RpcService>();
            if (rpcService == null)
            {
                throw new NotSupportedException("RPC plugin not available.");
            }
            rpcService.AddProcedure(route, handler, ordered);
        }

        /// <summary>
        /// Sends a remote procedure call using raw binary data as input, expecting no answer
        /// </summary>
        /// <param name="scene">The target scene. </param>
        /// <param name="route">The target route.</param>
        /// <param name="writer">A writer method writing the data to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>A task representing the remote procedure.</returns>
        public static Task RpcVoid(this Scene scene, string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return scene.Rpc(route, writer, priority).DefaultIfEmpty().ToVoidTask();
        }

        public static Task RpcVoid<TData>(this Scene scene, string route, TData data, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return scene.RpcVoid(route, stream => scene.Host.Serializer().Serialize(data, stream), priority);
        }

        /// <summary>
        /// Sends a remote procedure call using raw binary data as input and output, expecting exactly one answer
        /// </summary>
        /// <param name="scene">The target scene. </param>
        /// <param name="route">The target route.</param>
        /// <param name="writer">A writer method writing the data to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>A task representing the remote procedure, whose return value is the raw answer to the remote procedure call.</returns>
        public static Task<Packet<IScenePeer>> RpcTask(this Scene scene, string route, Action<Stream> writer, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return scene.Rpc(route, writer, priority).ToTask();
        }

        /// <summary>
        /// Sends a remote procedure call with an object as input, expecting exactly one answer
        /// </summary>
        /// <typeparam name="TData">The type of data to send</typeparam>
        /// <typeparam name="TResponse">The expected type of the responses.</typeparam>
        /// <param name="scene">The target scene.</param>
        /// <param name="route">The target route.</param>
        /// <param name="data">The data object to send.</param>
        /// <param name="priority">The priority level used to send the request.</param>
        /// <returns>A task representing the remote procedure, whose return value is the deserialized value of the answer</returns>
        public static Task<TResponse> RpcTask<TData, TResponse>(this Scene scene, string route, TData data, PacketPriority priority = PacketPriority.MEDIUM_PRIORITY)
        {
            return scene.RpcTask(route, s => scene.Host.Serializer().Serialize(data, s), priority)
                .Then(result => result.ReadObject<TResponse>());
        }
    }
}
