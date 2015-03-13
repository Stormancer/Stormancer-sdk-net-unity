module Stormancer {
    export class Scene implements IScene {
        public id: string;        
        
        // A byte representing the index of the scene for this peer.
        public handle: number;

        // A boolean representing whether the scene is connected or not.
        public connected: boolean;

        public hostConnection: IConnection;
        private _token: string;
        private _metadata: Map;
        private _remoteRoutesMap: IMap<Route> = {};
        private _localRoutesMap: IMap<Route> = {};
        private _client: Client;
        constructor(connection: IConnection, client: Client, id: string, token: string, dto: SceneInfosDto) {
            this.id = id;
            this.hostConnection = connection;
            this._token = token;
            this._client = client;
            this._metadata = dto.Metadata;

            for (var i = 0; i < dto.Routes.length; i++) {
                var route = dto.Routes[i];
                this._remoteRoutesMap[route.Name] = new Route(this, route.Name, route.Handle, route.Metadata);
            }
        }

        // Returns metadata informations for the remote scene host.
        getHostMetadata(key: string): string {
            return this._metadata[key];
        }
        
        // Registers a route on the local peer.
        public addRoute(route: string, handler: (packet: Packet<IScenePeer>) => void, metadata: Map = {}): void {
            if (route[0] === `@`) {
                throw new Error("A route cannot start with the @ character.");
            }

            if (this.connected) {
                throw new Error("You cannot register handles once the scene is connected.");
            }

            var routeObj = this._localRoutesMap[route];
            if (!routeObj) {
                routeObj = new Route(this, route, 0, metadata);
                this._localRoutesMap[route] = routeObj;
            }

            this.onMessage(route, handler);
        }

        public onMessage(route: string, handler: (packet: Packet<IScenePeer>) => void): void {
            if (this.connected) {
                throw new Error("You cannot register handles once the scene is connected.");
            }

            var routeObj = this._localRoutesMap[route];
            if (!routeObj) {
                routeObj = new Route(this, route);
                this._localRoutesMap[route] = routeObj;
            }

            this.onMessageImpl(routeObj, handler);
        }

        private onMessageImpl(route: Route, handler: (packet: Packet<IScenePeer>) => void): void {
            var index = route.index;

            var action = (p: Packet<IConnection>) => {
                var packet = new Packet(this.host(), p.data, p.getMetadata());
                handler(packet);
            };

            route.handlers.push(p => action(p));
        }

        // Sends a packet to the scene.
        public sendPacket(route: string, data: Uint8Array, priority: PacketPriority = PacketPriority.MEDIUM_PRIORITY, reliability: PacketReliability = PacketReliability.RELIABLE): void {
            if (!route) {
                throw new Error("route is null or undefined!");
            }
            if (!data) {
                throw new Error("data is null or undefind!");
            }
            if (!this.connected) {
                throw new Error("The scene must be connected to perform this operation.");
            }

            var routeObj = this._remoteRoutesMap[route];
            if (!routeObj) {
                throw new Error("The route " + route + " doesn't exist on the scene.");
            }

            this.hostConnection.sendToScene(this.handle, routeObj.index, data, priority, reliability);
        }

        // Connects the scene to the server.
        public connect(): JQueryPromise<void> {
            return this._client.connectToScene(this, this._token, Helpers.mapValues(this._localRoutesMap))
                .then(() => {
                this.connected = true;
            });
        }

        // Disconnects the scene.
        public disconnect(): JQueryPromise<void> {
            return this._client.disconnectScene(this, this.handle);
        }

        public handleMessage(packet: Packet<IConnection>): void {
            var ev = this.packetReceived;
            ev && ev.map((value) => {
                value(packet);
            });
            
            // extract the route id
            var routeId = new Uint16Array(packet.data.buffer, packet.data.byteOffset, 2)[0];

            packet.setMetadataValue("routeId", routeId);

            var observer = this._handlers[routeId];
            observer && observer.map(value => {
                value(packet);
            });
        }

        public completeConnectionInitialization(cr: ConnectionResult): void {
            this.handle = cr.SceneHandle;

            for (var key in this._localRoutesMap) {
                var route = this._localRoutesMap[key];
                route.index = cr.RouteMappings[key];
                this._handlers[route.index] = route.handlers;
            }
        }

        private _handlers: IMap<((packet: Packet<IConnection>) => void)[]> = {};

        // Fires when packet are received on the scene.
        packetReceived: ((packet: Packet<IConnection>) => void)[];

        public host(): IScenePeer {
            return new ScenePeer(this.hostConnection, this.handle, this._remoteRoutesMap, this);
        }
    }
}
