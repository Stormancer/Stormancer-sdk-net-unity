using System;
using System.Collections.Generic;
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

       
        /// <summary>
        /// Sends a request to the remote scene on the route, serializing the data with the scene serializer.
        /// </summary>
        /// <typeparam name="T">Type of the input request data.</typeparam>
        /// <typeparam name="U">Type of the output request data.</typeparam>
        /// <param name="scene">The remote scene proxy to which the request will be sent.</param>
        /// <param name="route">A String containing the name of the route to which the request should be sent.</param>
        /// <param name="data">The Input request data.</param>
        /// <returns>An observable outputting the request responses.</returns>
        public static IObservable<U> SendRequest<T, U>(this Scene scene, string route, T data)
        {
            return scene.SendRequest(route, s =>
            {
                scene.Host.Serializer().Serialize(data, s);
            }).Select(packet =>
            {
                var value = scene.Host.Serializer().Deserialize<U>(packet.Stream);
               
                return value;
            });
        }

        public static Task SendVoidRequest<T>(this Scene scene, string route, T data)
        {
            var tcs = new TaskCompletionSource<Unit>();
            scene.SendRequest(route, s =>
            {
                scene.Host.Serializer().Serialize(data, s);
            }).Subscribe(p => { }, () => tcs.SetResult(Unit.Default));

            return tcs.Task;
        }

        public static Task SendVoidRequest(this Scene scene, string route)
        {
            var tcs = new TaskCompletionSource<Unit>();
            scene.SendRequest(route, s =>
            {
            }).Subscribe(p => { }, () => tcs.SetResult(Unit.Default));

            return tcs.Task;
        }
        public static IObservable<T> SendRequest<T>(this Scene scene, string route)
        {
            return scene.SendRequest(route, s =>
            {
            }).Select(packet =>
            {
                var value = packet.Serializer().Deserialize<T>(packet.Stream);
                
                return value;
            });
        }

        public static void Send<T>(this Scene scene, string route, T data)
        {
            scene.SendPacket(route, s =>
            {
                scene.HostConnection.Serializer().Serialize(data, s);
            });
        }
    }
}
