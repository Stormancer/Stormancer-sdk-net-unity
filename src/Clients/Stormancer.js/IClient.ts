/// <reference path="Stormancer.ts" />

module Stormancer {
    export interface IClient {
        // The name of the Stormancer server application the client is connected to.
        applicationName: string;

        // An user specified logger.
        _logger: ILogger;

        // Returns a public scene (accessible without authentication)
        getPublicScene<T>(sceneId: string, userData: T): JQueryPromise<IScene>;

        // Returns a private scene (requires a token obtained from strong authentication with the Stormancer API.
        getScene(token: string): JQueryPromise<IScene>;

        // Disconnects the client.
        disconnect(): void;

        // The client's unique stormancer Id. Returns null if the Id has not been acquired yet (connection still in progress).
        id: number;

        // The name of the transport used for connecting to the server.
        serverTransportType: string;

        //// Returns statistics about the connection to the server.
        //getServerConnectionStatistics(): IConnectionStatistics;
    }
}
