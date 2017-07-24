using Stormancer.Plugins;
using Stormancer.Plugins.GameVersion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{

    public class GameVersionPlugin : IClientPlugin
    {
        internal const string METADATA_KEY = "stormancer.gameVersion";

        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += SceneCreated;
        }

        private void SceneCreated(Scene scene)
        {
            var pluginName = scene.GetHostMetadata(METADATA_KEY);
            if (!string.IsNullOrEmpty(pluginName))
            {
                var service = new GameVersionService(scene);
                scene.DependencyResolver.RegisterComponent(service);
            }
        }
    }
}
