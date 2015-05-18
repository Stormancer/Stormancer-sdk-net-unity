using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    /// <summary>
    /// Contract for a host plugin that interfaces with the .NET 45 server host.
    /// </summary>
    public interface IHostPlugin
    {
        /// <summary>
        /// Initializes the plugin and allows it to register to hosting events.
        /// </summary>
        /// <param name="ctx">A context provided by the host.</param>
        void Build(HostPluginBuildContext ctx);
    }
}
