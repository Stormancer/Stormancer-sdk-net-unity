module Stormancer {
    export interface IConnectionManager {
        // Generates an unique connection id for this node.
        generateNewConnectionId(): number;

        // Adds a connection to the manager
        newConnection(connection: IConnection): void;

        // Closes the target connection.
        closeConnection(connection: IConnection, reason: string): void;

        // Returns a connection by id.
        getConnection(id: number): IConnection;
    }
}
