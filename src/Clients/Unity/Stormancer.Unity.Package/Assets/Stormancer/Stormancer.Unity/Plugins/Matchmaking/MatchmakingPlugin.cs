using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{
    public class MatchmakingPlugin : IClientPlugin
    {
        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += SceneCreated;
        }

        private void SceneCreated(Scene scene)
        {
            var pluginName = scene.GetHostMetadata("stormancer.plugins.matchmaking");
            if(!string.IsNullOrEmpty(pluginName))
            {
                var matchmakingService = new MatchmakingService(scene);
                scene.DependencyResolver.RegisterComponent(matchmakingService);
            }

            pluginName = scene.GetHostMetadata("stormancer.gamesession");
            if(!string.IsNullOrEmpty(pluginName))
            {
                var gameSessionService = new GameSessionService(scene);
                scene.DependencyResolver.RegisterComponent(gameSessionService);
            }
        }
    }
}
