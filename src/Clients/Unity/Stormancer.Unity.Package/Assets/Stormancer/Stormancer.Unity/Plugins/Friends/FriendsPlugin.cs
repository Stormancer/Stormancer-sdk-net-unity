using Stormancer.Plugins;
using Stormancer.Plugins.Friends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{

    public class FriendsPlugin :IClientPlugin
    {
        internal const string METADATA_KEY = "stormancer.friends";

        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += SceneCreated;
        }

        private void SceneCreated(Scene scene)
        {
            var pluginName = scene.GetHostMetadata(METADATA_KEY);
            if (!string.IsNullOrEmpty(pluginName))
            {
                var friendsService = new FriendsService(scene);
                scene.DependencyResolver.RegisterComponent(friendsService);
            }
        }
    }
}
