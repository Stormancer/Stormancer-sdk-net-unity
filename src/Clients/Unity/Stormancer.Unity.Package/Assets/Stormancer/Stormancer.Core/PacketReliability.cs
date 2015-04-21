using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// Different available reliability levels when sending a packet.
    /// </summary>
    public enum PacketReliability
    {
        /// <summary>
        /// The packet may be lost, or arrive out of order. There are no guarantees whatsoever.
        /// </summary>
        UNRELIABLE = 0,
        /// <summary>
        /// The packets arrive in order, but may be lost. If a packet arrives out of order, it is discarded.
        /// The last packet may also never arrive.
        /// </summary>
        UNRELIABLE_SEQUENCED = 1,
        /// <summary>
        /// The packets always reach destination, but may do so out of order.
        /// </summary>
        RELIABLE = 2,
        /// <summary>
        /// The packets always reach destination and in order.
        /// </summary>
        RELIABLE_ORDERED = 3,
        /// <summary>
        /// The packets arrive at destination in order. If a packet arrive out of order, it is ignored.
        /// That mean that packets may disappear, but the last one always reach destination.
        /// </summary>
        RELIABLE_SEQUENCED = 4,

        /*
        UNRELIABLE_WITH_ACK_RECEIPT = 5,
        RELIABLE_WITH_ACK_RECEIPT = 6,
        RELIABLE_ORDERED_WITH_ACK_RECEIPT = 7,*/
    }
}
