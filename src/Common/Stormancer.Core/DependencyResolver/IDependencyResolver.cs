using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Dependency resolver interface for Stormancer.
    /// </summary>
    public interface IDependencyResolver : IDisposable
    {
        /// <summary>
        /// Resolves a single object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Resolve<T>() where T : class;

        /// <summary>
        /// Resolves all the registrations for the given contract.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<T> ResolveAll<T>() where T : class;

        /// <summary>
        /// Creates a child dependency resolver.
        /// </summary>
        /// <returns>The child dependency resolver.</returns>
        IDependencyResolver CreateChild();

        /// <summary>
        /// Creates a named child dependency resolver.
        /// </summary>
        /// <param name="name">The name of the child dependency resolver to create.</param>
        /// <returns>The child dependency resolver.</returns>
        IDependencyResolver CreateChild(string name);

        /// <summary>
        /// Creates a child dependency resolver.
        /// </summary>
        /// <param name="configurationAction">An action configuring the dependency resolver to add new dependencies to the child resolver.</param>
        /// <returns>The child dependency resolver.</returns>
        IDependencyResolver CreateChild(Action<IDependencyBuilder> configurationAction);

        /// <summary>
        /// Creates a named child dependency resolver.
        /// </summary>
        /// <param name="name">The name of the child dependency resolver to create.</param>
        /// <param name="configurationAction">An action configuring the dependency resolver to add new dependencies to the child resolver.</param>
        /// <returns>The child dependency resolver.</returns>
        IDependencyResolver CreateChild(string name, Action<IDependencyBuilder> configurationAction);
    }
}
