using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{

    /// <summary>
    /// Infos required to select a partition for an object.
    /// </summary>
    public class PartitioningDescriptor
    {
        /// <summary>
        /// Creates a partitioningDescriptor object.
        /// </summary>
        public PartitioningDescriptor()
        {
            Data = new Dictionary<string, byte[]>();
        }

        /// <summary>
        /// Id of the object (used by default)
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Additionnal partitioning informations
        /// </summary>
        public Dictionary<string, byte[]> Data { get; set; }

    }

    /// <summary>
    /// Abstract parent class for peer filters
    /// </summary>
    /// <remarks>
    /// Peer filters are used to select the peers that will receive a packet sent by the scene host.
    /// </remarks>
    public abstract class PeerFilter
    {
        /// <summary>
        /// Type of the filter
        /// </summary>
        public string Type
        {
            get
            {
                return this.GetType().Name;
            }
        }

    }

    /// <summary>
    /// Matches a scene
    /// </summary>
    public sealed class MatchSceneFilter: PeerFilter
    {
        /// <summary>
        /// Creates a MatchSceneFilter object
        /// </summary>
        /// <param name="id">scene id</param>
        public MatchSceneFilter(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Scene id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Targeted shard (optional)
        /// </summary>
        public uint? ShardId { get; set; }
        /// <summary>
        /// partition key (optional)
        /// </summary>
        /// <remarks>
        /// Shard &amp; partition key cannot be both set .
        /// </remarks>
        public PartitioningDescriptor PartitionKey { get; set; }
    }

    /// <summary>
    /// Matches the specified peer
    /// </summary>
    public sealed class MatchPeerFilter : PeerFilter
    {
        /// <summary>
        /// Creates a new MatchPeerFilter object
        /// </summary>
        /// <param name="id">Id to match</param>
        public MatchPeerFilter(long id)
        {
            Id = id;
        }

        /// <summary>
        /// Creates a new MatchPeerFilter object
        /// </summary>
        /// <param name="peer">Peer to match</param>
        public MatchPeerFilter(IScenePeerClient peer)
            : this(peer.Id)
        {

        }

        /// <summary>
        /// Id of the peer to match
        /// </summary>
        public long Id { get; private set; }
    }

    /// <summary>
    /// Match all peers connected to the scene
    /// </summary>
    public sealed class MatchAllFilter : PeerFilter { }

    /// <summary>
    /// Matches all the peers in the list.
    /// </summary>
    public sealed class MatchArrayFilter : PeerFilter
    {
        /// <summary>
        /// Creates an `InFilter` instance from an enumerable of peers
        /// </summary>
        /// <param name="peers">An `IEnumerable&lt;IScenePeerClient&gt;` object representing the list of peer to match.</param>
        public MatchArrayFilter(IEnumerable<IScenePeerClient> peers)
            : this(peers.Select(p => p.Id))
        {

        }
        /// <summary>
        /// Creates an `InFilter` instance from an enumerable of connection ids.
        /// </summary>
        /// <param name="ids">An `IEnumerable&lt;long&gt;` object representing the list of connection ids to match.</param>
        public MatchArrayFilter(IEnumerable<long> ids)
        {
            Ids = ids.ToArray();
        }

        /// <summary>
        /// Connection ids that are matched by the filter.
        /// </summary>
        public long[] Ids { get; private set; }
    }
}
