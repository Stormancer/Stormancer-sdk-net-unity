module Stormancer {
    export class ScenePeer implements IScenePeer {
        private _connection: IConnection;
        private _sceneHandle: number;
        private _routeMapping: IMap<Route>;
        private _scene: IScene;

        public id(): number {
            return this._connection.id;
        }

        public constructor(connection: IConnection, sceneHandle: number, routeMapping: IMap<Route>, scene: IScene) {
            this._connection = connection;
            this._sceneHandle = sceneHandle;
            this._routeMapping = routeMapping;
            this._scene = scene;
        }

        public send(route: string, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability) {
            var r = this._routeMapping[route];
            if (!r) {
                throw new Error("The route " + route + " is not declared on the server.");
            }
            this._connection.sendToScene(this._sceneHandle, r.index, data, priority, reliability);
        }
    }
}
