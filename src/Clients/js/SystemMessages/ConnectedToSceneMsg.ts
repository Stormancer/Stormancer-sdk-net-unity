module Stormancer {
    export interface ConnectToSceneMsg {
        Token: string;
        Routes: RouteDto[];
        ConnectionMetadata: Map;
    }
}
