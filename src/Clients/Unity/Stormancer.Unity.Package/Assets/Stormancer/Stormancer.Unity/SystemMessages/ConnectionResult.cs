using System.Collections.Generic;

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
