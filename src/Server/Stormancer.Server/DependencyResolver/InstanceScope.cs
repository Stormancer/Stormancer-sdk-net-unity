using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server.DependencyResolver
{
    /// <summary>
    /// This enum dictates how a dependency will be instantiated by the dependency resolver.
    /// </summary>
    public enum InstanceScope
    {
        /// <summary>
        /// The dependency will be generated anew at each call to Resolve (default).
        /// </summary>
        PerDependency,
        /// <summary>
        /// The dependency will be shared between all calls to Resolve.
        /// </summary>
        Single,
        /// <summary>
        /// The dependency will be created once for each life time scope.
        /// </summary>
        PerLifeTimeScope,
        /// <summary>
        /// The dependency will be created once for each life time scope of a given level.
        /// </summary>
        PerNamedLifeTimeScope
    }
}
