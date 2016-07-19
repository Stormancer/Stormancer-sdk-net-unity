using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server.Components
{
    /// <summary>
    /// Enables a server app to acquire unique resources.
    /// </summary>
    /// <remarks>
    ///  Pools are scoped to the app host. Different apps can share the same pool names, 
    ///  as well as different versions of the same app running simultaneously.
    /// </remarks>
    interface IResourcePools
    {
        /// <summary>
        /// Declares a named pool of integer resources
        /// </summary>
        /// <param name="name">Name of the pool</param>
        /// <param name="min">Mininum value in the pool (inclusive)</param>
        /// <param name="max">Maximum value in the pool (inclusive)</param>
        /// <param name="isClusterWide">Is the pool cluster wide or different on each node.</param>
        /// <returns>A Task completing when the pool is created</returns>
        Task DeclarePool(string name, int min, int max, bool isClusterWide = false);

        /// <summary>
        /// Acquires a resource in the pool.
        /// </summary>
        /// <param name="pool">Name of the pool from which to acquire the resource.</param>
        /// <returns>A lease containing the acquired resource. Dispose the lease to release the resource.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The requested pool does not exist. The pool must be declared before use.</exception>
        Task<ILease> AcquireResource(string pool);
    }

    /// <summary>
    /// An acquired value from a pool. Dispose this object to release the value.
    /// </summary>
    interface ILease : IDisposable
    {
        /// <summary>
        /// Value acquired in the pool is Success is true. 0 otherwise.
        /// </summary>
        int Value { get; }

        /// <summary>
        /// True if a resource could be acquired, false otherwise. 
        /// </summary>
        /// <remarks>If a resource couldn't be acquired, it may mean that the pool is empty.</remarks>
        bool Success { get; }
    }
}
