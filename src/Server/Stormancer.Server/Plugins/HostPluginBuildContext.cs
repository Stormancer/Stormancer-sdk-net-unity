using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    /// <summary>
    /// Context used by plugins to register actions that interact with the host runtime
    /// </summary>
    public class HostPluginBuildContext
    {
        /// <summary>
        /// Fired just before the scene template is applied to a new scene.
        /// </summary>
        public Action<ISceneHost> SceneCreating { get; set; }

        /// <summary>
        /// Fired just after the scene template is applied to a new scene.
        /// </summary>
        public Action<ISceneHost> SceneCreated { get; set; }

        /// <summary>
        /// Fired after a new scene has started.
        /// </summary>
        public Action<ISceneHost> SceneStarted { get; set; }
    }
}
