using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server.Components
{
    /// <summary>
    /// Provides details about remote peers.
    /// </summary>
    public interface IPeerInfosService
    {
        /// <summary>
        /// Get details about a peer from the server.
        /// </summary>
        /// <param name="connectionId">The id of the peer's connection.</param>
        /// <returns>
        /// An object containing details about the peer.
        /// </returns>
        Task GetPeerDetails(long connectionId);

        /// <summary>
        /// Get details about a peer from the server.
        /// </summary>
        /// <param name="peer">The peer object.</param>
        /// <returns>An object containing details about the peer.</returns>
        Task GetPeerDetails(IScenePeer peer);
       
    }

    /// <summary>
    /// Object containing details about a peer.
    /// </summary>
    public class PeerDetails
    {

    }
}
