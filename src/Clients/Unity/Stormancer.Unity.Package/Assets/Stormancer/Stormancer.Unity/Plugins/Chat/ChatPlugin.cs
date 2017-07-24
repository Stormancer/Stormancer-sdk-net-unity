using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stormancer;
using Stormancer.Plugins.Chat;

namespace Stormancer.Plugins
{
    public class ChatPlugin : IClientPlugin
    {
        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += SceneCreated;
        }

        private void SceneCreated(Scene scene)
        {
            if (!string.IsNullOrEmpty(scene.GetHostMetadata("stormancer.chat")))
            {
                var chat = new ChatService(scene);
                scene.DependencyResolver.RegisterComponent(chat);
            }
        }
    }
}
