using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if !StormancerClient
using Stormancer.Platform.Core.Composition;
#endif
namespace Stormancer.Networking
{
    /// <summary>
    /// A Stormancer network transport
    /// </summary>
#if !StormancerClient
    [Dependency]
#endif
    public interface ITransport : IDisposable
    {
        /// <summary>
        /// Starts the transport
        /// </summary>
        /// <param name="name">The name of the transport if several are started</param>
        /// <param name="handler">The connection handler used by the connection.</param>
        /// <param name="token">A `CancellationToken`. It will be cancelled when the transport has to be shutdown.</param>
        /// <param name="port">a `ushort?` indicating on which port the transport should listen if it is started as a server. null if it's a client.</param>
        /// <param name="maxConnections">The maximum number of simultaneous connections this transport can connect too. On clients, this restricts the number of P2P peers.</param>
        /// <returns>A `Task` completing when the transport is started.</returns>
        /// <remarks>
        /// Only server compatible transports support the `port` parameter. 
        /// </remarks>
        void Start(string name, IConnectionManager handler, CancellationToken token, ushort? port, ushort maxConnections);

        /// <summary>
        /// Gets a boolean indicating if the transport is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Connects the transport to a remote host.
        /// </summary>
        /// <param name="endpoint">a string containing the target endpoint the expected format is `host:port`</param>
        /// <returns>A `Task&lt;IConnection&gt;` object completing with the connection process and returning the corresponding `IConnection`</returns>
        Task<IConnection> Connect(string endpoint);

        /// <summary>
        /// Fires when the transport recieves new packets.
        /// </summary>
        Action<Packet> PacketReceived { get; set; }

        /// <summary>
        /// Fires when a remote peer has opened a connection.
        /// </summary>
        Action<IConnection> ConnectionOpened { get; set; }

        /// <summary>
        /// Fires when a connection to a remote peer is closed.
        /// </summary>
        Action<IConnection> ConnectionClosed { get; set; }

        /// <summary>
        /// The name of the transport.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Id of the local peer
        /// </summary>
        /// <remarks>
        /// The id can be null if the peer is not connected.
        /// </remarks>
        long? Id { get; }

    }
}
