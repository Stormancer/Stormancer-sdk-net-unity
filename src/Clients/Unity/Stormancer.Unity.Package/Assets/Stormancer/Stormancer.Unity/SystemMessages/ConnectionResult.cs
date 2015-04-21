using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
    public class ConnectionResult
    {
        public ConnectionResult() { }
        internal ConnectionResult(byte sceneHandle, Dictionary<string, ushort> routeMappings)
        {
            this.SceneHandle = sceneHandle;
            this.RouteMappings = routeMappings;
        }
        public byte SceneHandle { get; set; }
        public Dictionary<string, ushort> RouteMappings { get; set; }

    }
}
