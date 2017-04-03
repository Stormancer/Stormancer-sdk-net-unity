using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{
    public class TransactionBrokerPlugin : IClientPlugin
    {
        public void Build(PluginBuildContext ctx)
        {
            ctx.SceneCreated += OnSceneCreated;
        }

        private void OnSceneCreated(Scene scene)
        {
            var name = scene.GetHostMetadata("stormancer.turnByTurn");

            if(!string.IsNullOrEmpty(name))
            {
                var service = new TransactionBrokerService(scene);
                scene.DependencyResolver.RegisterComponent(service);
            }
        }
    }
}
