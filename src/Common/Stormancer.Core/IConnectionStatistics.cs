using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{
    public interface IConnectionStatistics
    {
        /// <summary>
        /// Number of packets lost in the last second.
        /// </summary>
        float PacketLossRate { get; }

        /// <summary>
        /// Get the kind of limitation on the outgoing flux.
        /// </summary>
        /// <remarks>Values can be:
        /// - None: the outgoing flux is not limited.
        /// - CongestionControl : the outgoing flux is limited by congestion control.
        /// - OutgoingBandwidth : the outgoing fluw is limited by the outgoing bandwidth.
        /// </remarks>
        BPSLimitationType BytesPerSecondLimitationType { get; }

        /// <summary>
        /// If the outgoing flux is limited, gets the limit rate.
        /// </summary>
        long BytesPerSecondLimit { get; }

        /// <summary>
        /// Gets the number of bytes in the sending queue.
        /// </summary>
        double QueuedBytes { get; }

        /// <summary>
        /// Gets the number of bytes in the sending queue for a given priority.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        double QueuedBytesForPriority(PacketPriority priority);

        /// <summary>
        /// Gets the number of packets in the sending queue.
        /// </summary>
        int QueuedPackets { get; }

        /// <summary>
        /// Gets the number of packets in the sending queue for a given priority.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        int QueuedPacketsForPriority(PacketPriority priority);
    }
}
