using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stormancer;

namespace Stormancer.Plugins
{
    public class ServerClockPlugin : IClientPlugin
    {
        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += SceneCreated;
        }

        private void SceneCreated(Scene scene)
        {
            if (!string.IsNullOrEmpty(scene.GetHostMetadata("stormancer.clock")))
            {
                var serverClock = new ServerClock(scene);
                scene.DependencyResolver.RegisterComponent(serverClock);
            }
        }
    }
}
