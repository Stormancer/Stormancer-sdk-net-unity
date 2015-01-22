using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Plugins
{
    interface IClientPlugin
    {
        void Build(PluginBuildContext ctx);
    }
}
