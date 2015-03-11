module Stormancer {
    export class ConnectionHandler implements IConnectionManager {
        private _current = 0;

        // Generates an unique connection id for this node.
        public generateNewConnectionId(): number {
            return this._current++;
        }

        // Adds a connection to the manager
        public newConnection(connection: IConnection): void { }

        // Returns a connection by id.
        public getConnection(id: number): IConnection {
            throw new Error("Not implemented.");
        }

        // Closes the target connection.
        public closeConnection(connection: IConnection, reason: string): void { }
    }

    export class Client implements IClient {

        private _apiClient: ApiClient;
        private _accountId: string;
        private _applicationName: string;

        //private _logger: ILogger = Logger.instance;

        private _transport: ITransport;
        private _dispatcher: IPacketDispatcher;

        private _initialized: boolean;

        private _tokenHandler: ITokenHandler = new TokenHandler();

        private _requestProcessor: RequestProcessor;
        private _scenesDispatcher: SceneDispatcher;

        private _serializers: IMap<ISerializer> = { msgpack: new MsgPackSerializer() };

        private _cts: Cancellation.tokenSource;

        private _metadata: Map;

        public applicationName: string;

        public _logger: ILogger;

        constructor(config: Configuration) {
            this._accountId = config.account;
            this._applicationName = config.application;
            this._apiClient = new ApiClient(config, this._tokenHandler);
            this._transport = config.transport;
            this._dispatcher = config.dispatcher;
            this._requestProcessor = new RequestProcessor(this._logger, []);

            this._scenesDispatcher = new SceneDispatcher();
            this._dispatcher.addProcessor(this._requestProcessor);
            this._dispatcher.addProcessor(this._scenesDispatcher);
            this._metadata = config.metadata;

            for (var i in config.serializers) {
                var serializer = config.serializers[i];
                this._serializers[serializer.name] = serializer;
            }

            this._metadata["serializers"] = Helpers.mapKeys(this._serializers).join(',');
            this._metadata["transport"] = this._transport.name;
            this._metadata["version"] = "1.0.0a";
            this._metadata["platform"] = "JS";

            this.initialize();
        }

        private initialize(): void {
            if (!this._initialized) {
                this._initialized = true;
                this._transport.packetReceived.push(packet => this.transportPacketReceived(packet));
            }
        }

        private transportPacketReceived(packet: Packet<IConnection>): void {
            this._dispatcher.dispatchPacket(packet);
        }

        public getPublicScene<T>(sceneId: string, userData: T): JQueryPromise<IScene> {
            return this._apiClient.getSceneEndpoint(this._accountId, this._applicationName, sceneId, userData)
                .then(ci => this.getSceneImpl(sceneId, ci));
        }

        public getScene(token: string): JQueryPromise<IScene> {
            var ci = this._tokenHandler.decodeToken(token);
            return this.getSceneImpl(ci.tokenData[7], ci);
        }

        private getSceneImpl(sceneId: string, ci: SceneEndpoint): JQueryPromise<IScene> {
            var self = this;
            return this.ensureTransportStarted(ci).then(() => {
                var parameter: SceneInfosRequestDto = { Metadata: self._serverConnection.metadata, Token: ci.token };
                return self.sendSystemRequest<SceneInfosRequestDto, SceneInfosDto>(MessageIDTypes.ID_GET_SCENE_INFOS, parameter);
            }).then((result: SceneInfosDto) => {
                if (!self._serverConnection.serializer) {
                    if (!result.SelectedSerializer) {
                        throw new Error("No serializer selected.");
                    }
                    self._serverConnection.serializer = self._serializers[result.SelectedSerializer];
                    self._serverConnection.metadata["serializer"] = result.SelectedSerializer;
                }
                var scene = new Scene(self._serverConnection, self, sceneId, ci.token, result);
                return scene;
            });
        }

        private sendSystemRequest<T, U>(id: number, parameter: T): JQueryPromise<U> {
            return this._requestProcessor.sendSystemRequest(this._serverConnection, id, this._systemSerializer.serialize(parameter))
                .then(packet => this._systemSerializer.deserialize<U>(packet.data));
        }

        private _systemSerializer: ISerializer = new MsgPackSerializer();

        private ensureTransportStarted(ci: SceneEndpoint): JQueryPromise<void> {
            var self = this;
            return Helpers.promiseIf(self._serverConnection == null,() => {
                return Helpers.promiseIf(!self._transport.isRunning, self.startTransport, self)
                    .then(() => {
                    return self._transport.connect(ci.tokenData[3][self._transport.name])
                        .then(c => self.registerConnection(c));
                });
            }, self);
        }

        private startTransport(): JQueryPromise<void> {
            this._cts = new Cancellation.tokenSource();
            return this._transport.start("client", new ConnectionHandler(), this._cts.token);
        }

        private registerConnection(connection: IConnection) {
            this._serverConnection = connection;
            for (var key in this._metadata) {
                this._serverConnection.metadata[key] = this._metadata[key];
            }
        }

        private _serverConnection: IConnection;

        public disconnectScene(scene: IScene, sceneHandle: number): JQueryPromise<void> {
            return this.sendSystemRequest(MessageIDTypes.ID_DISCONNECT_FROM_SCENE, sceneHandle)
                .then(() => this._scenesDispatcher.removeScene(sceneHandle));
        }

        public disconnect(): void {
            if (this._serverConnection) {
                this._serverConnection.close();
            }
        }

        public connectToScene(scene: Scene, token: string, localRoutes: Route[]): JQueryPromise<void> {
            var parameter: ConnectToSceneMsg = {
                Token: token,
                Routes: [],
                ConnectionMetadata: this._serverConnection.metadata
            };

            for (var i = 0; i < localRoutes.length; i++) {
                var r = localRoutes[i];
                parameter.Routes.push({
                    Handle: r.index,
                    Metadata: r.metadata,
                    Name: r.name
                });
            }

            return this.sendSystemRequest<ConnectToSceneMsg, ConnectionResult>(MessageIDTypes.ID_CONNECT_TO_SCENE, parameter)
                .then(result => {
                scene.completeConnectionInitialization(result);
                this._scenesDispatcher.addScene(scene);
            });
        }

        public id: number;

        public serverTransportType: string;
    }
}
