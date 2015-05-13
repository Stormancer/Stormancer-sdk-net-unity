using Stormancer.Diagnostics;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    /// <summary>
    /// Server plugin that receives and save client logs with the server logs.
    /// </summary>
    /// <remarks>Client logs' categories are automatically prefixed by 'client.' to help with filtering.</remarks>
    public class ClientLogsPlugin : IHostPlugin
    {
        internal const string Version = "1.0.0";
        internal const string PluginName = "stormancer.plugins.logs";

        /// <summary>
        /// Builds the plugin
        /// </summary>
        /// <param name="ctx">HostPluginBuildContext instance used to build the plugin.</param>
        public void Build(HostPluginBuildContext ctx)
        {
            ctx.SceneCreating += scene =>
            {

                scene.Metadata.Add(PluginName, Version);

                scene.AddRoute("logs.send", p =>
                {

                    var logger = scene.GetComponent<ILogger>();
                    var log = p.ReadObject<LogMsg>();
                    logger.Log(log.Level, "client." + log.Category, log.Message, log.Data);
                });
            };
        }
    }

    /// <summary>
    /// A log message sent by the client.
    /// </summary>
    public class LogMsg
    {
        /// <summary>
        /// Criticality of the log.
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Category of the log
        /// </summary>
        /// <remarks>
        /// Will be automatically prefixed by 'client.'
        /// </remarks>
        public string Category { get; set; }

        /// <summary>
        /// The message of the log.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional data included in the log.
        /// </summary>
        public Dictionary<string, string> Data { get; set; }
    }
}
