using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    /// <summary>
    /// Object passed to the Build method of plugins to register to the available Stormancer client events.
    /// </summary>
    public class PluginBuildContext
    {
        /// <summary>
        /// Event fired when the dependency resolver of a scene is configured.
        /// </summary>
        public Action<Scene,IDependencyBuilder> BuildingSceneResolver
        {
            get; set;
        }

        public Action<IDependencyBuilder> BuildingClientResolver { get; set; }
        /// <summary>
        /// Event fired when a scene object is created.
        /// </summary>
        public Action<Scene> SceneCreated { get; set; }

        /// <summary>
        /// Event fired when a client object is created.
        /// </summary>
        public Action<Client> ClientCreated { get; set; }

        /// <summary>
        /// Event fired when a a scene is connected to the server.
        /// </summary>
        public Action<Scene> SceneConnected { get; set; }
     
        /// <summary>
        /// Event fired when a scene is disconnected.
        /// </summary>
        public Action<Scene> SceneDisconnected { get; set; }
      
        /// <summary>
        /// Event fired when a packet is received from a remote peer.
        /// </summary>
        public Action<Packet> PacketReceived { get; set; }
      
    }
}
