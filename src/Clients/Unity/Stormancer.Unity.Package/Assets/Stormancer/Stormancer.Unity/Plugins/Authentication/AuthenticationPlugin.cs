using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{
    public class AuthenticationPlugin : IClientPlugin
    {
        public void Build(PluginBuildContext ctx)
        {
            ctx.ClientCreated += ClientCreated;
        }

        private void ClientCreated(Client client)
        {
            var authenticationService = new AuthenticationService(client);
            client.DependencyResolver.RegisterComponent(authenticationService);
        }
    }
}
