using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Plugins
{
    public class PluginBuildContext
    {
        public Action<Scene> SceneCreated { get; set; }
    }
}
