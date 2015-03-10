/// <reference path="Stormancer.ts" />

module Stormancer {
    // A Stormancer network transport
    interface ITransport {
        // Starts the transport
        start(type: string, handler: IConnectionManager, token: Cancellation.token): JQueryPromise<void>;

        // Gets a boolean indicating if the transport is currently running.
        isRunning: boolean;

        // Connects the transport to a remote host.
        connect(endpoint: string): JQueryPromise<IConnection>;

        // Fires when the transport recieves new packets.
        packetReceived: ((packet: Packet<IConnection>) => void)[];

        // Fires when a remote peer has opened a connection.
        connectionOpened: ((connection: IConnection) => void)[];

        // Fires when a connection to a remote peer is closed.
        connectionClosed: ((connection: IConnection) => void)[];

        // The name of the transport.
        name: string;

        id: Uint8Array;
    }
}
