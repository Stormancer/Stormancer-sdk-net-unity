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
        Task<PeerDetails> GetPeerDetails(long connectionId);

        /// <summary>
        /// Get details about a peer from the server.
        /// </summary>
        /// <param name="peer">The peer object.</param>
        /// <returns>An object containing details about the peer.</returns>
        Task<PeerDetails> GetPeerDetails(IScenePeer peer);

        /// <summary>
        /// Creates a token to establish a P2P connection to the provided peerId.
        /// </summary>
        /// <param name="peerId">Id of the peer</param>
        /// <returns></returns>
        Task<string> CreateP2pToken(long peerId);
       
    }

    /// <summary>
    /// Object containing details about a peer.
    /// </summary>
    public class PeerDetails
    {
        /// <summary>
        /// Continent code.
        /// </summary>
        public string Continent { get; set; }

        /// <summary>
        /// A Datetime instance containing the connection time of the peer.
        /// </summary>
        public DateTime ConnectedOn { get; set; }

        /// <summary>
        /// Ip address of the peer.
        /// </summary>
        public string IPAddress { get; set; }


        /// <summary>
        /// A boolean value indicating whether the server is able to provide Geo IP data.
        /// </summary>
        public bool GeoIpEnabled { get; set; }

        /// <summary>
        /// Country code.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// City code.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Latitude of the peer's location.
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Longitude of the peer's location.
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Time zone of the peer.
        /// </summary>
        public string TimeZone { get; set; }
    }
}
