using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{
    public class LeaderboardPlugin : IClientPlugin
    {
        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += SceneCreated;
        }

        private void SceneCreated(Scene scene)
        {
            var pluginName = scene.GetHostMetadata("stormancer.leaderboard");
            if (!string.IsNullOrEmpty(pluginName))
            {
                var leaderboardService = new LeaderboardService(scene);
                scene.DependencyResolver.RegisterComponent(leaderboardService);
            }
        }
    }
}
