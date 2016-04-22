using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server.DependencyResolver
{
    /// <summary>
    /// Used to specify instance scoping for a registration.
    /// </summary>
    /// <remarks>User code should not use this class.</remarks>
    public class InstanceScopeConfig
    {
        /// <summary>
        /// The scope of the registration.
        /// </summary>
        public InstanceScope Scope { get; set; }

        /// <summary>
        /// The name of the scope registration, if applicable.
        /// </summary>
        public string Name { get; set; }
    }
}
