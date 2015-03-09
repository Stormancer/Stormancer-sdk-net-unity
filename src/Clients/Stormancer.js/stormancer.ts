/// <reference path="Scripts/typings/msgpack/msgpack.d.ts" />
/// <reference path="CancellationTokenSource.ts" />

// Module
module Stormancer {
    export class MessageIDTypes {
        public static ID_CONNECT_TO_SCENE = 134;
        public static ID_DISCONNECT_FROM_SCENE = 135;
        public static ID_GET_SCENE_INFOS = 136;
        public static ID_REQUEST_RESPONSE_MSG = 137;
        public static ID_REQUEST_RESPONSE_COMPLETE = 138;
        public static ID_REQUEST_RESPONSE_ERROR = 139;
        public static ID_CONNECTION_RESULT = 140;
        public static ID_SCENES = 141;
    }

    export class jQueryWrapper {
        static $: JQueryStatic;
        static initWrapper(jquery: JQueryStatic) {
            jQueryWrapper.$ = jquery;
        }
    }

    // Available packet priorities
    export enum PacketPriority {
        // The packet is sent immediately without aggregation.
        IMMEDIATE_PRIORITY = 0,
        // The packet is sent at high priority level.
        HIGH_PRIORITY = 1,
        // The packet is sent at medium priority level.
        MEDIUM_PRIORITY = 2,
        // The packet is sent at low priority level.
        LOW_PRIORITY = 3
    }

    /// Different available reliability levels when sending a packet.
    export enum PacketReliability {
        /// The packet may be lost, or arrive out of order. There are no guarantees whatsoever.
        UNRELIABLE = 0,
        /// The packets arrive in order, but may be lost. If a packet arrives out of order, it is discarded.
        /// The last packet may also never arrive.
        UNRELIABLE_SEQUENCED = 1,
        /// The packets always reach destination, but may do so out of order.
        RELIABLE = 2,
        /// The packets always reach destination and in order.
        RELIABLE_ORDERED = 3,
        /// The packets arrive at destination in order. If a packet arrive out of order, it is ignored.
        /// That mean that packets may disappear, but the last one always reach destination.
        RELIABLE_SEQUENCED = 4
    }

    export enum ConnectionState {
        Disconnected,
        Connecting,
        Connected
    }

    // Represents the configuration of a Stormancer client.
    export class Configuration {
        constructor() {
            //this.dispatcher = new PacketDispatcher();
            this.transport = new WebSocketTransport();
            this.serializers = [];
            this.serializers.push(new MsgPackSerializer());
        }

        static apiEndpoint: string = "http://api.stormancer.com/";

        static localDevEndpoint: string = "http://localhost:8081/";

        // A boolean value indicating if the client should try to connect to the local dev platform.
        public isLocalDev: boolean;

        // A string containing the target server endpoint.
        // This value overrides the *IsLocalDev* property.
        public serverEndpoint: string;

        // A string containing the account name of the application.
        public account: string;

        // A string containing the name of the application.
        public application: string;

        getApiEndpoint(): string {
            if (this.isLocalDev) {
                return this.serverEndpoint ? this.serverEndpoint : Configuration.localDevEndpoint;
            }
            else {
                return this.serverEndpoint ? this.serverEndpoint : Configuration.apiEndpoint;
            }
        }

        // Creates a ClientConfiguration object targeting the local development server.
        static forLocalDev(applicationName: string): Configuration {
            var config = new Configuration();
            config.isLocalDev = true;
            config.application = applicationName;
            config.account = "local";
            return config;
        }

        // Creates a ClientConfiguration object targeting the public online platform.
        static forAccount(accountId: string, applicationName: string): Configuration {
            var config = new Configuration();
            config.isLocalDev = false;
            config.account = accountId;
            config.application = applicationName;
            return config;
        }

        public _metadata: Map = {};

        // Adds metadata to the connection.
        public Metadata(key: string, value: string): Configuration {
            this._metadata[key] = value;
            return this;
        }

        // Gets or Sets the dispatcher to be used by the client.
        public dispatcher: IPacketDispatcher;

        // Gets or sets the transport to be used by the client.
        public transport: ITransport;

        // List of available serializers for the client.
        // When negotiating which serializer should be used for a given remote peer, the first compatible serializer in the list is the one prefered.
        public serializers: ISerializer[];
    }

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

        private _serializers: IMap<ISerializer> = {};

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
            this._metadata = config._metadata;

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
                this._transport.packetReceived.push(this.transportPacketReceived);
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
            return this.getSceneImpl(ci.tokenData.sceneId, ci);
        }

        private getSceneImpl(sceneId: string, ci: SceneEndpoint): JQueryPromise<IScene> {
            return this.ensureTransportStarted(ci).then(() => {
                var parameter: SceneInfosRequestDto = { Metadata: this._serverConnection.metadata, Token: ci.token };
                return this.sendSystemRequest<SceneInfosRequestDto, SceneInfosDto>(MessageIDTypes.ID_GET_SCENE_INFOS, parameter);
            }).then((result: SceneInfosDto) => {
                if (!this._serverConnection.serializer) {
                    if (!result.SelectedSerializer) {
                        throw new Error("No serializer selected.");
                    }
                    this._serverConnection.serializer = this._serializers[result.SelectedSerializer];
                    this._serverConnection.metadata["serializer"] = result.SelectedSerializer;
                }
                var scene = new Scene(this._serverConnection, this, sceneId, ci.token, result);
                return scene;
            });
        }

        private sendSystemRequest<T, U>(id: number, parameter: T): JQueryPromise<U> {
            return this._requestProcessor.sendSystemRequest(this._serverConnection, id, this._systemSerializer.serialize(parameter))
                .then(packet => this._systemSerializer.deserialize<U>(packet.data));
        }

        private _systemSerializer: ISerializer = new MsgPackSerializer();

        private ensureTransportStarted(ci: SceneEndpoint): JQueryPromise<void> {
            return Helpers.promiseIf(this._serverConnection == null,() => {
                return Helpers.promiseIf(!this._transport.isRunning, this.startTransport)
                    .then(() => {
                    return this._transport.connect(ci.tokenData.endpoints[this._transport.name])
                        .then(this.registerConnection);
                });
            });
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

    export interface ConnectToSceneMsg {
        Token: string;
        Routes: RouteDto[];
        ConnectionMetadata: Map;
    }

    export interface ConnectionResult {
        SceneHandle: number;
        RouteMappings: IMap<number>;
    }

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
        addRoute(route: string, handler: (packet: Packet<IScenePeer>) => void, metadata: Map): void;

        // Sends a packet to the scene.
        sendPacket(route: string, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability, channel: number): void;

        // Disconnects the scene.
        disconnect(): JQueryPromise<void>;

        // Connects the scene to the server.
        connect(): JQueryPromise<void>;

        // Fires when packet are received on the scene.
        packetReceived: ((packet: Packet<IConnection>) => void)[];

        host(): IScenePeer;

        onMessage(route: string, handler: (packet: Packet<IScenePeer>) => void): void;
    }

    class Packet<T> {
        constructor(source: T, data: Uint8Array, metadata: IMap<any>) {
            this.source = source;
            this.data = data;
            this.metadata = metadata;
        }

        public source: T;

        // Data contained in the packet.
        public data: Uint8Array;

        public metadata: IMap<any>;

        public getMetadata(key: string): any {
            return this.metadata[key];
        }

        public connection: T;
    }

    class SceneDispatcher implements IPacketProcessor {
        private _scenes: Scene[] = [];

        public registerProcessor(config: PacketProcessorConfig): void {
            config.addCatchAllProcessor(this.handler);
        }

        private handler(sceneHandler: number, packet: Packet<IConnection>): boolean {
            if (sceneHandler < MessageIDTypes.ID_SCENES) {
                return false;
            }
            var scene = this._scenes[sceneHandler - MessageIDTypes.ID_SCENES];
            if (!scene) {
                return false;
            } else {
                packet.metadata["scene"] = scene;
                scene.handleMessage(packet);
                return true;
            }
        }

        public addScene(scene: Scene): void {
            this._scenes[scene.handle - MessageIDTypes.ID_SCENES] = scene;
        }

        public removeScene(sceneHandle: number) {
            delete this._scenes[sceneHandle - MessageIDTypes.ID_SCENES];
        }
    }

    // A remote scene.
    export interface IScenePeer {
        // Sends a message to the remote scene.
        send(route: string, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability): void;

        id(): number;
    }

    interface IPacketDispatcher {
        dispatchPacket(packet: Packet<IConnection>): void;

        addProcessor(processor: IPacketProcessor): void;
    }

    interface SceneInfosRequestDto {
        Token: string;
        Metadata: Map;
    }

    interface SceneInfosDto {
        SceneId: string;
        Metadata: Map;
        Routes: RouteDto[];
        SelectedSerializer: string;
    }

    interface RouteDto {
        Name: string;
        Handle: number;
        Metadata: Map;
    }

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
        connectionClose: ((connection: IConnection) => void)[];

        // The name of the transport.
        name: string;

        id: number;
    }

    // Contract for the binary serializers used by Stormancer applications.
    interface ISerializer {
        // Serialize an object into a stream.
        serialize<T>(data: T): Uint8Array;

        // Deserialize an object from a stream.
        deserialize<T>(bytes: Uint8Array): T;

        // The serializer format.
        name: string;
    }

    interface ILogger {
        trace(message: string): void;

        debug(message: string): void;

        error(ex: ExceptionInformation): void;

        error(format: string): void;

        info(format: string): void;
    }

    //interface IConnectionStatistics {
    //    /// Number of packets lost in the last second.
    //    packetLossRate: number;

    //    // Get the kind of limitation on the outgoing flux.
    //    bytesPerSecondLimitationType: BPSLimitationType;

    //    // If the outgoing flux is limited, gets the limit rate.
    //    bytesPerSecondLimit: number;

    //    // Gets the number of bytes in the sending queue.
    //    queuedBytes: number;

    //    // Gets the number of bytes in the sending queue for a given priority.
    //    queuedBytesForPriority(priority: PacketPriority): number;

    //    // Gets the number of packets in the sending queue.
    //    queuedPackets: number;

    //    // Gets the number of packets in the sending queue for a given priority.
    //    queuedPacketsForPriority(priority: PacketPriority): number;
    //}

    interface IConnection {

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
        sendToScene(sceneIndex: number, route: number, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability, channel: number): void;

        // Event fired when the connection has been closed
        connectionClosed: ((reason: string) => void)[];

        setApplication(account: string, application: string): void;

        // The connection's Ping in milliseconds
        //ping: number;

        // Returns advanced statistics about the connection.
        //getConnectionStatistics(): IConnectionStatistics;

        serializer: ISerializer;
    }

    interface IConnectionManager {
        // Generates an unique connection id for this node.
        generateNewConnectionId(): number;

        // Adds a connection to the manager
        newConnection(connection: IConnection): void;

        // Closes the target connection.
        closeConnection(connection: IConnection, reason: string): void;

        // Returns a connection by id.
        getConnection(id: number): IConnection;
    }

    interface IPacketProcessor {
        registerProcessor(config: PacketProcessorConfig): void;
    }

    // Contains method to register handlers for message types when passed to the IPacketProcessor.RegisterProcessor method.
    class PacketProcessorConfig {
        constructor(handlers: IMap<(packet: Packet<IConnection>) => boolean>, defaultprocessors: ((n: number, p: Packet<IConnection>) => boolean)[]) {
            this._handlers = handlers;
            this._defaultProcessors = defaultprocessors;
        }

        private _handlers: IMap<(packet: Packet<IConnection>) => boolean>;

        private _defaultProcessors: ((n: number, p: Packet<IConnection>) => boolean)[];

        // Adds an handler for the specified message type.
        public addProcessor(msgId: number, handler: (p: Packet<IConnection>) => boolean): void {
            if (this._handlers[msgId]) {
                throw new Error("An handler is already registered for id " + msgId);
            }
            this._handlers[msgId] = handler;
        }

        // Adds
        public addCatchAllProcessor(handler: (n: number, p: Packet<IConnection>) => boolean): void {
            this._defaultProcessors.push(handler);
        }
    }

    interface ITokenHandler {
        decodeToken(token: string): SceneEndpoint;
    }

    class TokenHandler implements ITokenHandler {
        private _tokenSerializer: ISerializer;

        public _tokenHandler(): void {
            this._tokenSerializer = new MsgPackSerializer();
        }

        public decodeToken(token: string): SceneEndpoint {
            var data = token.split('-')[0];
            var buffer = Helpers.base64ToByteArray(data);
            var result = this._tokenSerializer.deserialize<ConnectionData>(buffer);

            var sceneEndpoint = new SceneEndpoint();
            sceneEndpoint.token = token;
            sceneEndpoint.tokenData = result;
            return sceneEndpoint;
        }
    }

    class SceneEndpoint {
        public tokenData: ConnectionData;

        public token: string;
    }

    class ConnectionData {
        public endpoints: Map;

        public accountId: string;

        public application: string;

        public sceneId: string;

        public routing: string;

        public issued: Date;

        public expiration: Date;

        public userData: number[];

        public contentType: string;
    }

    class ApiClient {
        constructor(config: Configuration, tokenHandler: ITokenHandler) {
            this._config = config;
            this._tokenHandler = tokenHandler;
        }

        private _config: Configuration;
        private createTokenUri = "{0}/{1}/scenes/{2}/token";
        private _tokenHandler: ITokenHandler;

        public getSceneEndpoint<T>(accountId: string, applicationName: string, sceneId: string, userData: T): JQueryPromise<SceneEndpoint> {
            var serializer = new MsgPackSerializer();
            var data: Uint8Array = serializer.serialize(userData);

            var url = this._config.getApiEndpoint() + Helpers.stringFormat(this.createTokenUri, accountId, applicationName, sceneId);
            return $.ajax({
                type: "POST",
                url: url,
                contentType: "application/msgpack",
                headers: {
                    "Accept": "application/json",
                    "x-version": "1.0.0"
                },
                data: data
            }).then(result => {
                return this._tokenHandler.decodeToken(result);
            });
        }
    }

    class MsgPackSerializer implements ISerializer {
        public serialize<T>(data: T): Uint8Array {
            return new Uint8Array(msgpack.pack(data));
        }

        public deserialize<T>(bytes: Uint8Array): T {
            return msgpack.unpack(bytes);
        }

        name: string = "MsgPack";
    }

    interface IMap<T> {
        [key: string]: T;
    }

    interface Map {
        [key: string]: string;
    }

    class Helpers {
        static base64ToByteArray(data: string): Uint8Array {
            return new Uint8Array(atob(data).split('').map(function (v) { return parseInt(v) }));
        }

        static stringFormat(str: string, ...args: any[]): string {
            for (var i in args) {
                str = str.replace('{' + i + '}', args[i]);
            }
            return str;
        }

        static mapKeys(map: { [key: string]: any }): string[] {
            var keys: string[] = [];
            for (var key in map) {
                if (map.hasOwnProperty(key)) {
                    keys.push(key);
                }
            }
            return keys;
        }

        static mapValues<T>(map: IMap<T>): T[] {
            var result: T[] = [];
            for (var key in map) {
                result.push(map[key]);
            }

            return result;
        }

        static promiseFromResult<T>(result: T): JQueryPromise<T> {
            var deferred = jQuery.Deferred();
            deferred.resolve(result);
            return deferred.promise();
        }

        static promiseIf(condition: boolean, action: () => JQueryPromise<void>): JQueryPromise<void> {
            if (condition) {
                return action();
            } else {
                return Helpers.promiseFromResult(null);
            }
        }
    }

    interface IRequestModule {
        register(builder: (msgId: number, handler: (context: RequestContext) => JQueryPromise<void>) => void): void;
    }

    //export class RequestModuleBuilder {
    //    private _addHandler: (msgId: number, handler: (context: RequestContext) => JQueryPromise<void>) => void;

    //    private requestModuleBuilder(addHandler: (msgId: number, handler: (context: RequestContext) => JQueryPromise<void>) => void) {
    //        if (!addHandler) {
    //            throw new Error("addHandler is null or undefined.");
    //        }
    //        this._addHandler = addHandler;
    //    }

    //    public service(msgId: number, handler: (context: RequestContext) => JQueryPromise<void>): void {
    //        this._addHandler(msgId, handler);
    //    }
    //}

    export class RequestContext {
        private _packet: Packet<IConnection>;
        private _requestId: Uint8Array;
        private _didSendValues = false;
        public inputData: Uint8Array;
        public isComplete = false;

        constructor(p: Packet<IConnection>) {
            this._packet = p;
            this._requestId = p.data.subarray(0, 2);
            this.inputData = p.data.subarray(2);
        }

        public send(data: Uint8Array): void {
            if (this.isComplete) {
                throw new Error("The request is already completed.");
            }
            this._didSendValues = true;
            var dataToSend = new Uint8Array(2 + data.length);
            dataToSend.set(this._requestId);
            dataToSend.set(data, 2);
            this._packet.connection.sendSystem(MessageIDTypes.ID_REQUEST_RESPONSE_MSG, dataToSend);
        }

        public complete(): void {
            var dataToSend = new Uint8Array(3);
            dataToSend.set(this._requestId);
            dataToSend.set(2, this._didSendValues ? 1 : 0);
            this._packet.connection.sendSystem(MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE, dataToSend);
        }

        public error(data: Uint8Array): void {
            var dataToSend = new Uint8Array(2 + data.length);
            dataToSend.set(this._requestId);
            dataToSend.set(data, 2);
            this._packet.connection.sendSystem(MessageIDTypes.ID_REQUEST_RESPONSE_ERROR, dataToSend);
        }
    }

    export class RequestProcessor implements IPacketProcessor {
        private _pendingRequests: IMap<Request> = {};
        private _logger: ILogger;
        private _isRegistered: boolean = false;
        private _handlers: IMap<(context: RequestContext) => JQueryPromise<void>> = {};

        constructor(logger: ILogger, modules: IRequestModule[]) {
            this._pendingRequests = {};
            this._logger = logger;
            for (var key in modules) {
                var mod = modules[key];
                mod.register(this.addSystemRequestHandler);
            }
        }

        public registerProcessor(config: PacketProcessorConfig): void {
            this._isRegistered = true;
            for (var key in this._handlers) {
                var handler = this._handlers[key];
                config.addProcessor(key,(p: Packet<IConnection>) => {
                    var context = new RequestContext(p);

                    var continuation = (fault: any) => {
                        if (!context.isComplete) {
                            if (fault) {
                                context.error(p.connection.serializer.serialize(fault));
                            }
                            else {
                                context.complete();
                            }
                        }
                    };

                    handler(context)
                        .done(() => continuation(null))
                        .fail(error => continuation(error));

                    return true;
                });
            }
        }

        public addSystemRequestHandler(msgId: number, handler: (context: RequestContext) => JQueryPromise<void>): void {
            if (this._isRegistered) {
                throw new Error("Can only add handler before 'registerProcessor' is called.");
            }
            this._handlers[msgId] = handler;
        }

        private reserveRequestSlot(observer: IObserver<Packet<IConnection>>) {
            var id = 0;

            while (id < 65535) {
                if (!this._pendingRequests[id]) {
                    var request: Request = { lastRefresh: new Date, id: id, observer: observer, deferred: jQuery.Deferred<void>() };
                    this._pendingRequests[id] = request;
                    return request;
                }
                id++;
            }

            throw new Error("Unable to create new request: Too many pending requests.");
        }

        public sendSystemRequest(peer: IConnection, msgId: number, data: Uint8Array): JQueryPromise<Packet<IConnection>> {
            var deferred = $.Deferred<Packet<IConnection>>();

            var request = this.reserveRequestSlot({
                onNext(packet) { deferred.resolve(packet); },
                onError(e) { deferred.reject(e) },
                onCompleted() { }
            });

            peer.sendSystem(msgId, data);

            deferred.promise().always(() => {
                var r = this._pendingRequests[request.id];
                if (r == request) {
                    delete this._pendingRequests[request.id];
                }
            });

            return deferred.promise();
        }
    }

    export interface Request {
        lastRefresh: Date;
        id: number;
        observer: IObserver<Packet<IConnection>>;
        deferred: JQueryDeferred<void>;
    }

    export interface IObserver<T> {
        onCompleted(): void;
        onError(error: any): void;
        onNext(value: T): void;
    }

    export class Route {
        public handlers: ((packet: Packet<IConnection>) => void)[] = [];

        public constructor(public scene: IScene, public name: string, public index = 0, public metadata: Map = {}) {
        }
    }

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
                var packet = new Packet(this.host(), p.data, p.metadata);
                handler(packet);
            };

            route.handlers.push(action);
        }

        // Sends a packet to the scene.
        public sendPacket(route: string, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability, channel: number): void {
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

            this.hostConnection.sendToScene(this.handle, routeObj.index, data, priority, reliability, channel);
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
            var temp = packet.data.subarray(0, 2);
            var routeId = new Uint16Array(temp.buffer)[0];

            packet.metadata["routeId"] = routeId;

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
            this._connection.sendToScene(this._sceneHandle, r.index, data, priority, reliability, 0);
        }
    }

    export class WebSocketTransport implements ITransport {
        public name: string = "websocket";
        public id: number;
        // Gets a boolean indicating if the transport is currently running.
        public isRunning: boolean = false;

        private _type: string;
        private _connectionManager: IConnectionManager;
        private _socket: WebSocket;
        private _connecting = false;

        // Fires when the transport recieves new packets.
        public packetReceived: ((packet: Packet<IConnection>) => void)[] = [];

        // Fires when a remote peer has opened a connection.
        public connectionOpened: ((connection: IConnection) => void)[] = [];

        // Fires when a connection to a remote peer is closed.
        public connectionClose: ((connection: IConnection) => void)[] = [];
        
        // Starts the transport
        public start(type: string, handler: IConnectionManager, token: Cancellation.token): JQueryPromise<void> {
            this._type = name;
            this._connectionManager = handler;

            this.isRunning = true;

            token.onCancelled(this.stop);

            var deferred = $.Deferred<void>();
            deferred.resolve();
            return deferred.promise();
        }

        private stop() {
            this.isRunning = false;
            if (this._socket) {
                this._socket.close();
                this._socket = null;
            }
        }
        
        // Connects the transport to a remote host.
        public connect(endpoint: string): JQueryPromise<IConnection> {
            if (!this._socket && !this._connecting) {
                this._connecting = true;
                try {
                    var socket = new WebSocket("ws://" + endpoint + "/");
                    socket.binaryType = "arraybuffer";

                    socket.onopen = () => this.onOpen(socket);
                    socket.onmessage = args => this.onMessage(args.data);
                    socket.onclose = args => this.onClose(args.wasClean);
                    this._socket = socket;

                    var connection = this.createNewConnection(this._socket);
                    //TODO
                }
                finally {
                    this._connecting = false;
                }
                //TODO
            }
            throw "Not implemented";
            return $.Deferred().promise();
        }

        private createNewConnection(socket: WebSocket): WebSocketConnection {
            //TODO
            throw "Not Implemented";
        }

        private onOpen(socket: WebSocket) {
        }

        private onMessage(data: ArrayBuffer) {
            //TODO
        }

        private onClose(clean: boolean) {
            //TODO
        }
    }

    export class WebSocketConnection implements IConnection {
        private _socket: WebSocket;

        public constructor(id: number, socket: WebSocket) {
            this.id = id;
            this._socket = socket;
            this.connectionDate = new Date();
            this.state = ConnectionState.Connected;
        }

        // Unique id in the node for the connection.
        public id: number;

        // Connection date.
        public connectionDate: Date;

        // Metadata associated with the connection.
        public metadata: Map;

        // Account of the application which the peer is connected to.
        public account: string

        // Name of the application to which the peer is connected.
        public application: string;

        // State of the connection.
        public state: ConnectionState;

        // Close the connection
        public close(): void {
            this._socket.close();
        }

        // Sends a system message to the peer.
        public sendSystem(msgId: number, data: Uint8Array): void {
            var bytes = new Uint8Array(data.length + 1);
            bytes[0] = msgId;
            bytes.set(data, 1);

            this._socket.send(bytes.buffer);
        }
 
        // Sends a packet to the target remote scene.
        public sendToScene(sceneIndex: number, route: number, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability, channel: number): void {
            var bytes = new Uint8Array(data.length + 3);
            bytes[0] = sceneIndex;

            var ushorts = new Uint16Array(1);
            ushorts[0] = route;
            bytes.set(new Uint8Array(ushorts.buffer), 1);

            bytes.set(data, 3);

            this._socket.send(bytes.buffer);
        }

        // Event fired when the connection has been closed
        public connectionClosed: ((reason: string) => void)[];

        public setApplication(account: string, application: string): void {
            this.account = account;
            this.application = application;
        }

        public serializer: ISerializer;
    }
}

interface JQueryStatic {
    stormancer: (configuration: Stormancer.Configuration) => Stormancer.IClient;
}

(function ($, window) {
    Stormancer.jQueryWrapper.initWrapper($);
    $.stormancer = (configuration: Stormancer.Configuration) => { return new Stormancer.Client(configuration); };
    //jQuery.support.cors = true
} (jQuery, window));