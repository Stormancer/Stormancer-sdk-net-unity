using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// Represents a scene host.
    /// </summary>
    public interface ISceneHost : IScene
    {
        /// <summary>
        /// Adds a route handler to the scene
        /// </summary>
        /// <param name="route">The name of the route</param>
        /// <param name="handler">The route handler</param>
        /// <param name="metadata">Optional metadata</param>
        void AddRoute(string route, Action<Packet<IScenePeerClient>> handler, Dictionary<string, string> metadata = null);

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
         string Template { get;}

        /// <summary>
        /// True if the scene is accessible without secure tokens
        /// </summary>
         bool IsPublic { get;  }

        /// <summary>
        /// True if the scene is persistent
        /// </summary>
        /// <remarks>
        /// A persistent scene can be restarted after hibernation (for inactivity for instance). When a non persistent scene is destroyed, it's completely deleted from the cluster.
        /// </remarks>
         bool IsPersistent { get;  }

    }
}
