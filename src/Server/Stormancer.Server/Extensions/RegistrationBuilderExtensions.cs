using Stormancer.Server.DependencyResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{ 
    /// <summary>
    /// Provides server-specific extention methods to further control dependency registrations.
    /// </summary>
    public static class RegistrationBuilderExtensions
    {
        /// <summary>
        /// Marks the dependency as a single instance, that will be shared by the dependency resolver and its children.
        /// </summary>
        /// <param name="builder">The registration to modify.</param>
        /// <returns>The IRegistrationBuilder, for chaining purpose.</returns>
        public static IRegistrationBuilder SingleInstance(this IRegistrationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddOptions(data =>
            {
                data["InstanceScope"] = new InstanceScopeConfig { Scope = InstanceScope.Single };
            });
        }

        /// <summary>
        /// Marks the dependency as a single instance for each scene. The instances will not be shared between scenes, and the dependency will not be present outside of a scene.
        /// </summary>
        /// <param name="builder">The registration to modify.</param>
        /// <returns>The IRegistrationBuilder, for chaining purpose.</returns>
        public static IRegistrationBuilder InstancePerScene(this IRegistrationBuilder builder)
        {
            return builder.InstancePerNamedLifetimeScope("scene");
        }

        /// <summary>
        /// Marks the dependency as a single instance for each scope with the given name. The instances will not be shared between scopes, and a scope without the right name or without a parent with the right name will not contain the dependency..
        /// </summary>
        /// <param name="builder">The registration to modify.</param>
        /// <param name="name">The name of the scope.</param>
        /// <returns>The IRegistrationBuilder, for chaining purpose.</returns>
        public static IRegistrationBuilder InstancePerNamedLifetimeScope(this IRegistrationBuilder builder, string name)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddOptions(data =>
            {
                data["InstanceScope"] = new InstanceScopeConfig { Scope = InstanceScope.PerNamedLifeTimeScope, Name = name };
            });
        }
        

        /// <summary>
        /// Marks the dependency as a different instance for . The instances will not be shared between scopes. (default)
        /// </summary>
        /// <param name="builder">The registration to modify.</param>
        /// <returns>The IRegistrationBuilder, for chaining purpose.</returns>
        public static IRegistrationBuilder InstancePerDependency(this IRegistrationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddOptions(data =>
            {
                data["InstanceScope"] = InstanceScope.PerDependency;
            });
        }
    }
}
