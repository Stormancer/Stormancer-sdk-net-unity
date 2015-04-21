using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
    /// <summary>
    /// Dto representing the result of a connection attempt
    /// </summary>
    public class ConnectionResult
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionResult() { }
        internal ConnectionResult(byte sceneHandle, Dictionary<string, ushort> routeMappings)
        {
            this.SceneHandle = sceneHandle;
            this.RouteMappings = routeMappings;
        }

        /// <summary>
        /// Handle of the scene the client was connected to.
        /// </summary>
        public byte SceneHandle { get; set; }

        /// <summary>
        /// Route mappings in the scene (ie : routeName => routeHandle)
        /// </summary>
        public Dictionary<string, ushort> RouteMappings { get; set; }

    }
}
