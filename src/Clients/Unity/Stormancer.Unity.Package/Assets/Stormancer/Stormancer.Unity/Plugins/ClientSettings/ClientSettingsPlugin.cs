using Stormancer.Plugins.ClientSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{
    public class ClientSettingsPlugin : IClientPlugin
    {
        internal const string METADATA_KEY = "stormancer.clientsettings";

        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += SceneCreated;
        }

        private void SceneCreated(Scene scene)
        {
            var pluginName = scene.GetHostMetadata(METADATA_KEY);
            if (!string.IsNullOrEmpty(pluginName))
            {
                var clientSettingsService = new ClientSettingsService(scene);
                scene.DependencyResolver.RegisterComponent(clientSettingsService);
            }
        }
    }
}
