module Stormancer {
    export interface IConnectionStatistics {
        /// Number of packets lost in the last second.
        packetLossRate: number;

        // Get the kind of limitation on the outgoing flux.
        bytesPerSecondLimitationType: any;//BPSLimitationType;

        // If the outgoing flux is limited, gets the limit rate.
        bytesPerSecondLimit: number;

        // Gets the number of bytes in the sending queue.
        queuedBytes: number;

        // Gets the number of bytes in the sending queue for a given priority.
        queuedBytesForPriority(priority: PacketPriority): number;

        // Gets the number of packets in the sending queue.
        queuedPackets: number;

        // Gets the number of packets in the sending queue for a given priority.
        queuedPacketsForPriority(priority: PacketPriority): number;
    }
}
