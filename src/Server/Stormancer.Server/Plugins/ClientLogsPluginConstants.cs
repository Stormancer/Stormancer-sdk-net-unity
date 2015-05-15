using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    public static class ClientLogsPluginConstants
    {
        internal const string Version = "1.0.0";
        internal const string PluginName = "stormancer.plugins.logs";
        internal const string EnableClientLogsRoute = "stormancer.logs.enable";
        internal const string ClientLogsRoute = "stormancer.logs.send";
        internal const string isLoggingMetadataKey = "ClientLogs.ShouldSend";
    }
}
