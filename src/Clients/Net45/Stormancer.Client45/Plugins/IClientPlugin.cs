using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{
    /// <summary>
    /// A Stormancer plugin, adding behaviours to a Stormancer client.
    /// </summary>
    public interface IClientPlugin
    {
        /// <summary>
        /// Builds the plugin, registering all relevant events.
        /// </summary>
        /// <param name="ctx">The plugin build context on which you can register all relevant events.</param>
        void Build(PluginBuildContext ctx);
    }
}
