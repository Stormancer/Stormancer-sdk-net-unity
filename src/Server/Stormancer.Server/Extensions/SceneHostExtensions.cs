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
        /// Broadcasts a message to all clients.
        /// </summary>
        /// <param name="scene">The scene on which to broadcast.</param>
        /// <param name="route">The route of the message.</param>
        /// <param name="writer">An action that will write the message.</param>
        /// <param name="priority">The message priority level.</param>
        /// <param name="reliability">The message reliability level.</param>
        public static void Broadcast(this ISceneHost scene, string route, Action<Stream> writer, PacketPriority priority, PacketReliability reliability)
        {
            scene.Send(new MatchAllFilter(), route, writer, priority, reliability);

        }

        /// <summary>
        /// Listen to messages on the specified route, and output instances of T using the scene serializer.
        /// </summary>
        /// <typeparam name="TData">Type into which message contents should be deserialized.</typeparam>
        /// <param name="scene">The scene to listen to.</param>
        /// <param name="route">A string describing the route to listen to.</param>
        /// <returns>An observable instance providing the messages.</returns>
        public static IObservable<TData> OnMessage<TData>(this ISceneHost scene, string route)
        {
            return scene.OnMessage(route).Select(packet =>
            {
                var value = packet.Serializer().Deserialize<TData>(packet.Stream);

                return value;
            });
        }

        /// <summary>
        /// Sends an object to the target peer with the requested reliability and priority levels.
        /// </summary>
        /// <typeparam name="TData">The type of object to send.</typeparam>
        /// <param name="peer">The target peer.</param>
        /// <param name="route">The target route on the scene peer.</param>
        /// <param name="data">The data that will be serialized then sent.</param>
        /// <param name="priority">The priority level.</param>
        /// <param name="reliability">The reliability level.</param>
        public static void Send<TData>(this IScenePeer peer, string route, TData data, PacketPriority priority = PacketPriority.HIGH_PRIORITY, PacketReliability reliability = PacketReliability.RELIABLE_ORDERED)
        {
            peer.Send(route, s =>
            {
                peer.Serializer().Serialize(data, s);
            }, priority, reliability);
        }

        /// <summary>
        /// Broadcasts an object to all clients
        /// </summary>
        /// <typeparam name="TData">The type of object to send.</typeparam>
        /// <param name="scene">The scene on which to broadcast.</param>
        /// <param name="route">The target route on the scene peers.</param>
        /// <param name="data">The data that will be serialized then sent.</param>
        /// <param name="priority">The priority level.</param>
        /// <param name="reliability">The reliability level.</param>
        /// <remarks>
        /// This method uses the first peer's serializer to serialize the data. Do not use it if the peers connected to the scene use different serializers.
        /// </remarks>
        public static void BroadCast<TData>(this ISceneHost scene, string route, TData data, PacketPriority priority, PacketReliability reliability)
        {
            scene.Send(new MatchAllFilter(), route, s =>
            {
                var firstPeer = scene.RemotePeers.FirstOrDefault();
                if (firstPeer != null)
                {
                    firstPeer.Serializer().Serialize(data, s);
                }
            }, priority, reliability);
        }

        /// <summary>
        /// Listen to messages on the specified route, deserialize them and execute the given handler for eah of them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene">The scene on which the route messages will be listened.</param>
        /// <param name="route">The route to listen.</param>
        /// <param name="handler">The handler to execute for each message on the route.</param>
        /// <returns>An IDisposable object you can use to unregister the handler.</returns>
        public static IDisposable AddRoute<T>(this ISceneHost scene, string route, Action<T> handler)
        {
            return scene.OnMessage<T>(route).Subscribe(handler);
        }

        /// <summary>
        /// Listen to messages on the specified route, deserialize them and execute the given handler for eah of them. (Duplicate of AddRoute for compatibility)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene">The scene on which the route messages will be listened.</param>
        /// <param name="route">The route to listen.</param>
        /// <param name="handler">The handler to execute for each message on the route.</param>
        /// <returns>An IDisposable object you can use to unregister the handler.</returns>
        /// <remarks>RegisterRoute is an alias to the AddRoute method.</remarks>
        public static IDisposable RegisterRoute<T>(this ISceneHost scene, string route, Action<T> handler)
        {
            return scene.AddRoute(route, handler);
        }

        //public static void EnableClientLogs(this IScenePeerClient peer)
        //{
        //    peer.Host.GetComponent<Stormancer.Plugins.ClientLogsService>().EnableClientLogs(peer);
        //}

        //public static void DisableClientLogs(this IScenePeerClient peer)
        //{
        //    peer.Host.GetComponent<Stormancer.Plugins.ClientLogsService>().DisableClientLogs(peer);
        //}
    }
}
