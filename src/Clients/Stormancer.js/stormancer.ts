/// <reference path="Scripts/typings/msgpack/msgpack.d.ts" />
/// <reference path="CancellationTokenSource.ts" />

// Module
module Stormancer {
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
            //this.dispatcher = new DefaultPacketDispatcher();
            //this.transport = new RaknetTransport(NullLogger.Instance);
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
        logger: ILogger;

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
        private _sceneDispatcher: SceneDispatcher;

        private _serializers: IMap<ISerializer> = {};

        private _cts: Cancellation.tokenSource;

        private _metadata: Map;

        public applicationName: string;

        public logger: ILogger;

        constructor(config: Configuration) {
            this._accountId = config.account;
            this._applicationName = config.application;
            this._apiClient = new ApiClient(config, this._tokenHandler);
            this._transport = config.transport;
            this._dispatcher = config.dispatcher;
            this._requestProcessor = new Stormancer.Networking.Processors.RequestProcessor(this._logger, []);

            this._scenesDispatcher = new Processors.SceneDispatcher();
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
                var previous = this._transport.packetReceived;
                this._transport.packetReceived = previous ? (packet => {
                    previous(packet);
                    this.transport_packetReceived(packet);
                }) : this.transport_packetReceived;
            }
        }

        private transport_packetReceived(packet: Packet<IConnection>): void {
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
                return this.sendSystemRequest<SceneInfosRequestDto, SceneInfosDto>(136, parameter);
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
            return this._transport.start("client", new ConnectionHandler(), this._cts.token, null, 50);
        }

        private registerConnection(connection: IConnection) {
            this._serverConnection = connection;
            for (var key in this._metadata) {
                this._serverConnection.metadata[key] = this._metadata[key];
            }
        }

        private _serverConnection: IConnection;

        public disconnect(): void { }

        public id: number;

        public serverTransportType: string;
    }

    export class ConnectionHandler implements IConnectionManager {
        // Generates an unique connection id for this node.
        generateNewConnectionId(): number {
            throw "Not Implemented!";
        }

        // Adds a connection to the manager
        newConnection(connection: IConnection): void {
            throw "Not Implemented!";
        }

        // Closes the target connection.
        closeConnection(connection: IConnection, reason: string): void {
            throw "Not Implemented!";
        }

        // Returns a connection by id.
        getConnection(id: number): IConnection {
            throw "Not Implemented!";
        }
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
        disconnect: JQueryPromise<void>;

        // Connects the scene to the server.
        connect: JQueryPromise<void>;

        // Fires when packet are received on the scene.
        packetReceived: (packet: Packet<IConnection>) => void;

        host: IScenePeer;
    }

    class Packet<T> {
        constructor(source: T, data: Uint8Array, metadata: Map) {
            this.source = source;
            this.data = data;
            this.metadata = metadata;
        }

        public source: T;

        // Data contained in the packet.
        public data: Uint8Array;

        public metadata: Map;

        public getMetadata(key: string): string {
            return this.metadata[key];
        }

        public connection: T;
    }

    // A remote scene.
    export interface IScenePeer {
        // Sends a message to the remote scene.
        send(route: string, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability): void;

        id: number;
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
        start(type: string, handler: IConnectionManager, token: Cancellation.token, port: number, maxConnections: number): JQueryPromise<void>;

        // Gets a boolean indicating if the transport is currently running.
        isRunning: boolean;

        // Connects the transport to a remote host.
        connect(endpoint: string): JQueryPromise<IConnection>;

        // Fires when the transport recieves new packets.
        packetReceived: (packet: Packet<IConnection>) => void;

        // Fires when a remote peer has opened a connection.
        connectionOpened: (connection: IConnection) => void;

        // Fires when a connection to a remote peer is closed.
        connectionClose: (connection: IConnection) => void;

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

        // Ip address of the remote peer.
        ipAddress: string;

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

        sendRaw(data: Uint8Array, priority: PacketPriority, reliability: PacketReliability, channel: number): void;
 
        // Sends a packet to the target remote scene.
        sendToScene(sceneIndex: number, route: number, data: Uint8Array, priority: PacketPriority, reliability: PacketReliability, channel: number); void;

        // Event fired when the connection has been closed
        connectionClosed: ((reason: string) => void);

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

    //class ConnectionHandler implements IConnectionManager {
    //    private _current: number = 0;

    //    public generateNewConnectionId(): number {
    //        return this._current++;
    //    }

    //    public NewConnection(connection: IConnection): void {
    //    }

    //    public CloseConnection(connection: IConnection, reason: string): void {
    //    }

    //    public GetConnection(id: number): IConnection {
    //        throw new Error("Not implemented");
    //    }
    //}

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
            return jQueryWrapper.$.ajax({
                type: "POST",
                url: url,
                contentType: "application/msgpack",
                accepts: {
                    json: "application/json"
                },
                headers: {
                    "x-version": "1.0"
                },
                data: data
            }).fail(() => {
                throw new Error("TODO");
            });/*.done(() => {
                return this._tokenHandler.DecodeToken(response.ReadAsString());
            });*/
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

    export class RequestProcessor {
        private _pendingRequests: { [key: number]: Request } = {};

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

    export class Scene implements IScene {
        constructor(connection: IConnection, client: Client, id: string, token: string, dto: SceneInfosDto) {
        }


        //TODO !!!
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