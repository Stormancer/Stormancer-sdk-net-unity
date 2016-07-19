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
    public interface IDelegatedTransports
    {


        /// <summary>
        /// Acquires a resource in the pool.
        /// </summary>
        /// <param name="transport">Name of the transport from which to acquire the port.</param>
        /// <returns>A lease containing the acquired resource. Dispose the lease to release the resource.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The requested pool does not exist. The pool must be declared before use.</exception>
        Task<ILease> AcquirePort(string transport);
    }

    /// <summary>
    /// An acquired value from a pool. Dispose this object to release the value.
    /// </summary>
    public interface ILease : IDisposable
    {
        /// <summary>
        /// Public ip 
        /// </summary>
        string PublicIp { get; }
        /// <summary>
        /// Value acquired in the pool is Success is true. 0 otherwise.
        /// </summary>
        ushort Port { get; }

        /// <summary>
        /// True if a resource could be acquired, false otherwise. 
        /// </summary>
        /// <remarks>If a resource couldn't be acquired, it may mean that the pool is empty.</remarks>
        bool Success { get; }
    }
}
