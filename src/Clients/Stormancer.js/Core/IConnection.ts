/// <reference path="Stormancer.ts" />

module Stormancer {
    export enum ConnectionState {
        Disconnected,
        Connecting,
        Connected
    }

    export interface IConnection {
        // Unique id in the node for the connection.
        id: number;

        // Connection date.
        connectionDate: Date;

        // Metadata associated with the connection.
        metadata: Map;

        //// Register components.
        //RegisterComponent<T>(component: T): void;

        //// Gets a service from the object.
        //GetComponent<T>(): T;

        // Account of the application which the peer is connected to.
        account: string

        // Name of the application to which the peer is connected.
        application: string;

        // State of the connection.
        state: ConnectionState;

        // Close the connection
        close(): void;

        // Sends a system message to the peer.
        sendSystem(msgId: number, data: Uint8Array): void;
 
        // Sends a packet to the target remote scene.
        sendToScene(sceneIndex: number, route: number, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability): void;

        // Event fired when the connection has been closed
        connectionClosed: ((reason: string) => void)[];

        setApplication(account: string, application: string): void;

        // The connection's Ping in milliseconds
        //ping: number;

        // Returns advanced statistics about the connection.
        //getConnectionStatistics(): IConnectionStatistics;

        serializer: ISerializer;
    }
}
