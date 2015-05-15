using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    public class ClientLogsService
    {
        public Task EnableClientLogs(IScenePeerClient client)
        {
            return ToggleClientLogs(client, true);
        }

        public Task DisableClientLogs(IScenePeerClient client)
        {
            return ToggleClientLogs(client, false);
        }

        private Task ToggleClientLogs(IScenePeerClient client, bool shouldEnable)
        {
            string previousStateString;
            var previousState = client.Metadata.TryGetValue(ClientLogsPluginConstants.isLoggingMetadataKey, out previousStateString) ? previousStateString == "True" : false;

            if (previousState != shouldEnable)
            {
                client.Metadata[ClientLogsPluginConstants.isLoggingMetadataKey] = shouldEnable.ToString();
                return client.SendVoidRequest(ClientLogsPluginConstants.EnableClientLogsRoute, shouldEnable);
            }
            else
            {
                return Task.FromResult(System.Reactive.Unit.Default);
            }
        }
    }
}
