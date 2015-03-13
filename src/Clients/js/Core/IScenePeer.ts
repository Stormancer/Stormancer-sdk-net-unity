module Stormancer {
    // A remote scene.
    export interface IScenePeer {
        // Sends a message to the remote scene.
        send(route: string, data: Uint8Array, priority?: PacketPriority, reliability?: PacketReliability): void;

        id(): number;
    }
}
