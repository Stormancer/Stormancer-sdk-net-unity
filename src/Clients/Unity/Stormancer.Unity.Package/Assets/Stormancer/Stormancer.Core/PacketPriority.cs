using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// Available packet priorities
    /// </summary>
    /// <remarks>
    /// If packets are in competition for expedition, priority levels work as follow:
    /// 2 HIGH_PRIORITY packets are sent for 1 MEDIUM_PRIORITY packet.
    /// 2 MEDIUM_PRIORITY packets are sent for 1 LOW_PRIORITY packet.
    /// </remarks>
    public enum PacketPriority
    {
        /// <summary>
        /// The packet is sent immediately without aggregation.
        /// </summary>
        IMMEDIATE_PRIORITY = 0,
        /// <summary>
        /// The packet is sent at high priority level.
        /// </summary>
        HIGH_PRIORITY = 1,
        /// <summary>
        /// The packet is sent at medium priority level.
        /// </summary>
        MEDIUM_PRIORITY = 2,
        /// <summary>
        /// The packet is sent at low priority level.
        /// </summary>
        LOW_PRIORITY = 3
    }
}
