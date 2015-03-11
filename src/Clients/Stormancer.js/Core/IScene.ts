module Stormancer {
    export interface IScene {
        // Represents a Stormancer scene.
        id: string;

        // Returns metadata informations for the remote scene host.
        getHostMetadata(key: string): string;

        // A byte representing the index of the scene for this peer.
        handle: number;

        // A boolean representing whether the scene is connected or not.
        connected: boolean;

        hostConnection: IConnection;

        // Registers a route on the local peer.
        addRoute(route: string, handler: (packet: Packet<IScenePeer>) => void, metadata?: Map): void;

        // Sends a packet to the scene.
        sendPacket(route: string, data: Uint8Array, priority?: PacketPriority, reliability?: PacketReliability): void;

        // Disconnects the scene.
        disconnect(): JQueryPromise<void>;

        // Connects the scene to the server.
        connect(): JQueryPromise<void>;

        // Fires when packet are received on the scene.
        packetReceived: ((packet: Packet<IConnection>) => void)[];

        host(): IScenePeer;

        onMessage(route: string, handler: (packet: Packet<IScenePeer>) => void): void;
    }
}
