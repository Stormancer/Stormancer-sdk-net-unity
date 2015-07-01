using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Server;

namespace Stormancer.Plugins
{
    /// <summary>
    /// Context used by plugins to register actions that interact with the host runtime
    /// </summary>
    public class HostPluginBuildContext
    {
        /// <summary>
        /// Fires just before the scene template is applied to a new scene.
        /// </summary>
        public Action<ISceneHost> SceneCreating { get; set; }

        /// <summary>
        /// Fires just after the scene template is applied to a new scene.
        /// </summary>
        public Action<ISceneHost> SceneCreated { get; set; }

        /// <summary>
        /// Fires after a new scene has started.
        /// </summary>
        public Action<ISceneHost> SceneStarted { get; set; }

        /// <summary>
        /// Fires when the server host is starting. 
        /// </summary>
        public Action<IHost> HostStarting { get; set; }

        /// <summary>
        /// Fires when the server host is shutting down
        /// </summary>
        public Action<IHost> HostShuttingDown { get; set; }
    }
}
