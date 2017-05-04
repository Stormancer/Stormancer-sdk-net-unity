using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// Options when registering an handler to a route.
    /// </summary>
    public class RouteOptions : Dictionary<string, object>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public RouteOptions()
        {
            AutoDisposePacketStream();
        }
        /// <summary>
        /// The packet Stream should automatically be disposed at the end of the handler task.
        /// </summary>
        /// <param name="autoDispose"></param>
        /// <returns></returns>
        public RouteOptions AutoDisposePacketStream(bool autoDispose = true)
        {
            this["autoDisposeStream"] = autoDispose;
            return this;
        }

        /// <summary>
        /// Gets an option
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            return (T)this[key];
        }

        /// <summary>
        /// Sets an option
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string key, object value)
        {

        }
    }

    /// <summary>
    /// Represents a scene host.
    /// </summary>
    public interface ISceneHost : IScene
    {

        /// <summary>
        /// Adds a route handler for client messages.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="handler"></param>
        /// <param name="options">Route options.</param>
        /// <param name="metadata"></param>
        void AddRoute(string route, Func<Packet<IScenePeerClient>, Task> handler, Func<RouteOptions, RouteOptions> options, Dictionary<string, string> metadata = null);

        /// <summary>
        /// Adds a route handler for messages from other scene hosts.
        /// </summary>
        /// <param name="route"></param>
        /// <param name="handler"></param>
        /// <param name="options"></param>
        void AddInternalRoute(string route, Func<Packet<IScenePeer>, Task> handler, Func<RouteOptions, RouteOptions> options);

        /// <summary>
        /// Sends a packet to the selected scene client peers.
        /// </summary>
        /// <param name="filter">`PeerFilter` object matching the target peers.</param>
        /// <param name="route">The target route for the message.</param>
        /// <param name="writer">Stream writer providing the content of the packet.</param>
        /// <param name="priority">A `PacketPriority` instance representing the priority of the packet.</param>
        /// <param name="reliability">A `PacketReliability` instance representing the reliability level of the packet.</param>
        void Send(PeerFilter filter, string route, Action<Stream> writer, PacketPriority priority,
            PacketReliability reliability);

        #region events
        /// <summary>
        /// Event fired when the scene is starting
        /// </summary>
        /// <remarks>
        /// The scene metadata are provided to the event handler to enable customization.
        /// </remarks>
        ITaskBasedEventHandler<dynamic> Starting { get; }

        /// <summary>
        /// Event fired when the scene is shutting down
        /// </summary>
        /// <remarks>
        /// Scenes shutdown after a period of inactivity or when it is deleted. 
        /// Custom parameters can be provided using the 'x-userdata' header on the delete API call.
        /// </remarks>
        ITaskBasedEventHandler<ShutdownArgs> Shuttingdown { get; }

        /// <summary>
        /// Event fired when a new user try to connect to the scene.
        /// </summary>
        /// <remarks>
        /// Throwing an exception in this handler will cancel the connection attempt. 
        /// Throwing a 'ClientException' will send back the exception message to the client.
        /// </remarks>
        ITaskBasedEventHandler<IScenePeerClient> Connecting { get; }

        /// <summary>
        /// Event fired when a new user is connected to the scene.
        /// </summary>
        ITaskBasedEventHandler<IScenePeerClient> Connected { get; }

        /// <summary>
        /// Event fired when an user disconnects from the scene.
        /// </summary>
        ITaskBasedEventHandler<DisconnectedArgs> Disconnected { get; }


        /// <summary>
        /// List of scene peers connected to this scene. 
        /// </summary>
        IEnumerable<IScenePeerClient> RemotePeers { get; }

        #endregion
        /// <summary>
        /// Scene metadata
        /// </summary>
        Dictionary<string, string> Metadata { get; }

        /// <summary>
        /// The name of the template used to build the scene.
        /// </summary>
        string Template { get; }

        /// <summary>
        /// True if the scene is accessible without secure tokens
        /// </summary>
        bool IsPublic { get; }

        /// <summary>
        /// True if the scene is persistent
        /// </summary>
        /// <remarks>
        /// A persistent scene can be restarted after hibernation (for inactivity for instance). When a non persistent scene is destroyed, it's completely deleted from the cluster.
        /// </remarks>
        bool IsPersistent { get; }

        /// <summary>
        /// Runs a task on the thread pool whose lifecycle is linked with the scene.
        /// </summary>
        /// <param name="runAction">The method that will be run on the thread pool.</param>
        /// <remarks>The task will be forcibly stopped (with a `TaskCancelledException`) as soon as the scene closes.</remarks>
        Task RunTask(Func<Task> runAction);

        /// <summary>
        /// Runs a task on the thread pool whose lifecycle is linked with the scene.
        /// </summary>
        /// <param name="runAction">The method that will be run on the thread pool.</param>
        /// <remarks>The cancelation token provided to the task factory will be cancelled as soon as the scene closes. If it does not stop on its own then, the task will be forcibly stopped (with a `TaskCancelleException`) after 30s.</remarks>
        Task RunTask(Func<CancellationToken, Task> runAction);

        /// <summary>
        /// Creates an IObservable&lt;Packet&gt; instance that listen to events on the specified route.
        /// </summary>
        /// <param name="route">A string containing the name of the route to listen to.</param>
        /// <returns type="IObservable&lt;Packet&gt;">An IObservable&lt;Packet&gt; instance that fires each time a message is received on the route. </returns>
        IObservable<Packet<IScenePeerClient>> OnMessage(string route);
    }
}
