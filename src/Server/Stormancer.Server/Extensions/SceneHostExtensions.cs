using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Newtonsoft.Json;
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
        public static void Broadcast(this ISceneHost scene, string route, Action<Stream> writer, PacketPriority priority = PacketPriority.HIGH_PRIORITY, PacketReliability reliability = PacketReliability.RELIABLE_ORDERED)
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
        public static void Broadcast<TData>(this ISceneHost scene, string route, TData data, PacketPriority priority = PacketPriority.HIGH_PRIORITY, PacketReliability reliability = PacketReliability.RELIABLE_ORDERED)
        {
            var peersBySerializer = scene.RemotePeers.ToLookup(peer => peer.Serializer().Name);

            foreach (var group in peersBySerializer)
            {
                scene.Send(new MatchArrayFilter(group), route, s =>
                    {
                        group.First().Serializer().Serialize(data, s);
                    }, priority, reliability);
            }
        }

        /// <summary>
        /// Listen to messages on the specified route, deserialize them and execute the given handler for each of them.
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
        /// <summary>
        /// Deserializes user data using the relevant serializer
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="client">The scene peer client whose user data will be read from.</param>
        /// <returns>The deserialized user data.</returns>
        public static T GetUserData<T>(this IScenePeerClient client)
        {
            using (var userDataStream = new MemoryStream(client.UserData))
            {

                if (client.ContentType == "application/json")
                {
                    var serializer = new JsonSerializer();
                    using (var sr = new StreamReader(userDataStream))
                    {
                        using (var jsonTextReader = new JsonTextReader(sr))
                        {
                            return serializer.Deserialize<T>(jsonTextReader);
                        }
                    }
                }
                else
                {
                    return client.Serializer().Deserialize<T>(userDataStream);
                }
            }
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
