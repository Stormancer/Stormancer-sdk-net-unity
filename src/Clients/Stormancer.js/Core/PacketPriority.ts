/// <reference path="Stormancer.ts" />

module Stormancer {
    // Available packet priorities
    export enum PacketPriority {
        // The packet is sent immediately without aggregation.
        IMMEDIATE_PRIORITY = 0,
        // The packet is sent at high priority level.
        HIGH_PRIORITY = 1,
        // The packet is sent at medium priority level.
        MEDIUM_PRIORITY = 2,
        // The packet is sent at low priority level.
        LOW_PRIORITY = 3
    }
}
