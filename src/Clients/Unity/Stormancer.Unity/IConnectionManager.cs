#if !StormancerClient
using Stormancer.Platform.Core.Composition;
#endif
using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Networking
{
    /// <summary>
    /// Manages connections
    /// </summary>
#if !StormancerClient
    [Dependency]
#endif
    public interface IConnectionManager
    {
        /// <summary>
        /// Generates an unique connection id for this node.
        /// </summary>
        /// <returns>A `long` containing an unique id.</returns>
        /// <remarks>Only used on servers.</remarks>
        long GenerateNewConnectionId();

        /// <summary>
        /// Adds a connection to the manager
        /// </summary>
        /// <param name="connection">The connection object to add.</param>
        /// <remarks>This method is called by the infrastructure when a new connection connects to a transport.</remarks>
        void NewConnection(IConnection connection);

        /// <summary>
        /// Closes the target connection.
        /// </summary>
        /// <param name="connection">The connection to close.</param>
        void CloseConnection(IConnection connection, string reason);

        /// <summary>
        /// Returns a connection by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IConnection GetConnection(long id);
    }
}
