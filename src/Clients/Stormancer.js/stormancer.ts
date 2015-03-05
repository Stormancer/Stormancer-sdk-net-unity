/// <reference path="Scripts/typings/msgpack/msgpack.d.ts" />

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
            //this.serializers.push( new MsgPackSerializer() );
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

        //private _metadata: Dictionary = {};

        //// Adds metadata to the connection.
        //public Metadata(key: string, value: string): Configuration {
        //    this._metadata[key] = value;
        //    return this;
        //}

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
        getPublicScene<T>(sceneId: string, userData: T): Task<IScene>;

        // Returns a private scene (requires a token obtained from strong authentication with the Stormancer API.
        getScene(token: string): Task<IScene>;

        // Disconnects the client.
        disconnect(): void;

        // The client's unique stormancer Id. Returns null if the Id has not been acquired yet (connection still in progress).
        id: number;

        // The server connection's ping, in milliseconds.
        serverPing: number;

        // The name of the transport used for connecting to the server.
        serverTransportType: string;

        // Returns statistics about the connection to the server.
        getServerConnectionStatistics(): IConnectionStatistics;
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
        addRoute(route: string, handler: Action<Packet<IScenePeer>>, metadata: Dictionary): void;

        // Sends a packet to the scene.
        sendPacket(route: string, writer: Action<Stream>, priority: PacketPriority, reliability: PacketReliability, channel: number): void;

        // Disconnects the scene.
        disconnect: Task<void>;

        // Connects the scene to the server.
        connect: Task<void>;

        // Fires when packet are received on the scene.
        packetReceived: Action<Packet<IConnection>>;

        host: IScenePeer;
    }

    interface Stream {
        (): Uint8Array;
    }

    interface IDictionary<T> {
        [key: string]: T;
    }

    interface Dictionary {
        [key: string]: string;
    }

    interface Action<T> {
        T: void;
    }

    interface Task<T> {
        T: JQueryPromise<T>;
    }

    class Packet<T> {
        constructor(source: T, data: Uint8Array, metadata: Dictionary) {
            this.source = source;
            this.data = data;
            this.metadata = metadata;
        }

        public source: T;

        // Data contained in the packet.
        public data: Uint8Array;

        public metadata: Dictionary;

        public getMetadata(key: string): string {
            return this.metadata[key];
        }

        public connection: T;
    }

    // A remote scene.
    export interface IScenePeer {
        // Sends a message to the remote scene.
        send(route: string, writer: Action<Stream>, priority: PacketPriority, reliability: PacketReliability): void;

        id: number;
    }

    interface IPacketDispatcher {
        dispatchPacket(packet: Packet<IConnection>): void;

        addProcessor(processor: IPacketProcessor): void;
    }

    // A Stormancer network transport
    interface ITransport {
        // Starts the transport
        start(type: string, handler: IConnectionManager, token: CancellationToken, port: number, maxConnections: number): Task<void>;

        // Gets a boolean indicating if the transport is currently running.
        isRunning: boolean;

        // Connects the transport to a remote host.
        connect(endpoint: string): Task<IConnection>;

        // Fires when the transport recieves new packets.
        packetReceived: Action<Packet<IConnection>>;

        // Fires when a remote peer has opened a connection.
        connectionOpened: Action<IConnection>;

        // Fires when a connection to a remote peer is closed.
        connectionClose: Action<IConnection>;

        // The name of the transport.
        name: string;

        id: number;
    }

    // Contract for the binary serializers used by Stormancer applications.
    interface ISerializer {
        // Serialize an object into a stream.
        serialize<T>(data: T, stream: Stream): void;

        // Deserialize an object from a stream.
        deserialize<T>(stream: Stream): T;

        // The serializer format.
        name: string;
    }

    interface ILogger {
        trace(message: string, ...args: any[]): void;

        debug(message: string, ...args: any[]): void;

        error(ex: ExceptionInformation): void;

        error(format: string, ...args: any[]): void;

        info(format: string, ...args: any[]): void;
    }

    interface IConnectionStatistics {
        /// Number of packets lost in the last second.
        PacketLossRate: number;

        // Get the kind of limitation on the outgoing flux.
        BytesPerSecondLimitationType: BPSLimitationType;

        // If the outgoing flux is limited, gets the limit rate.
        BytesPerSecondLimit: number;

        // Gets the number of bytes in the sending queue.
        QueuedBytes: number;

        // Gets the number of bytes in the sending queue for a given priority.
        QueuedBytesForPriority(priority: PacketPriority): number;

        // Gets the number of packets in the sending queue.
        QueuedPackets: number;

        // Gets the number of packets in the sending queue for a given priority.
        QueuedPacketsForPriority(priority: PacketPriority): number;
    }

    interface IConnection {

        // Unique id in the node for the connection.
        Id: number;

        // Ip address of the remote peer.
        IpAddress: string;

        // Connection date.
        ConnectionDate: DateTime;

        // Metadata associated with the connection.
        Metadata: Dictionary;

        // Register components.
        RegisterComponent<T>(component: T): void;

        // Gets a service from the object.
        GetComponent<T>(): T;

        // Account of the application which the peer is connected to.
        Account: string

        // Name of the application to which the peer is connected.
        Application: string;

        // State of the connection.
        State: ConnectionState;

        // Close the connection
        Close(): void;

        // Sends a system message to the peer.
        SendSystem(msgId: number, writer: Action<Stream>): void;

        SendRaw(writer: Action<Stream>, priority: PacketPriority, reliability: PacketReliability, channel: number): void;
 
        // Sends a packet to the target remote scene.
        SendToScene(sceneIndex: number, route: number, writer: Action<Stream>, priority: PacketPriority, reliability: PacketReliability, channel: number); void;

        // Event fired when the connection has been closed
        ConnectionClosed: Action<string>

        SetApplication(account: string, application: string): void;

        // The connection's Ping in milliseconds
        Ping: number;

        // Returns advanced statistics about the connection.
        GetConnectionStatistics(): IConnectionStatistics;
    }

    interface IConnectionManager {
        // Generates an unique connection id for this node.
        generateNewConnectionId: number;

        // Adds a connection to the manager
        NewConnection(connection: IConnection): void;

        // Closes the target connection.
        CloseConnection(connection: IConnection, reason: string): void;

        // Returns a connection by id.
        GetConnection(id: number): IConnection;
    }

    interface IPacketProcessor {
        RegisterProcessor(config: PacketProcessorConfig): void;
    }

    // Contains method to register handlers for message types when passed to the IPacketProcessor.RegisterProcessor method.
    class PacketProcessorConfig {
        constructor(handlers: IDictionary<(packet: Packet<IConnection>, b: boolean) => void>, defaultprocessors: ((n: number, p: Packet<IConnection>, b: boolean) => void)[]) {
            this._handlers = handlers;
            this._defaultProcessors = defaultprocessors;
        }

        private _handlers: IDictionary<(packet: Packet<IConnection>, b: boolean) => void>;

        private _defaultProcessors: ((numb: number, packet: Packet<IConnection>, b: boolean) => void)[];

        // Adds an handler for the specified message type.
        public AddProcessor(msgId: number, handler: (p: Packet<IConnection>, b: boolean) => void): void {
            if (this._handlers[msgId]) {
                //throw new ArgumentException(string.Format("An handler is already registered for id {0}", msgId));
            }
            this._handlers[msgId] = handler;
        }

        // Adds
        public AddCatchAllProcessor(handler: (n: number, p: Packet<IConnection>, b: boolean) => void): void {
            this._defaultProcessors.push(handler);
        }
    }

    class CancellationToken {
    }

    class DateTime {
    }

    export class Client implements IClient {
        public applicationName: string;

        public logger: ILogger;

        public getPublicScene<T>(sceneId: string, userData: T): Task<IScene> {
            return;
        }

        public getScene(token: string): Task<IScene> {
            return;
        }

        public disconnect(): void {
        }

        public id: number;

        public serverPing: number;

        public serverTransportType: string;

        public getServerConnectionStatistics(): IConnectionStatistics {
            return;
        }
    }

    export class Scene implements IScene {
        public id: string;

        public getHostMetadata(key: string): string {
            return;
        }

        public handle: number;

        public connected: boolean;

        public hostConnection: IConnection;

        public addRoute(route: string, handler: Action<Packet<IScenePeer>>, metadata: Dictionary): void {
        }

        public sendPacket(route: string, writer: Action<Stream>, priority: PacketPriority, reliability: PacketReliability, channel: number): void {
        }

        public disconnect: Task<void>;

        public connect: Task<void>;

        public packetReceived: Action<Packet<IConnection>>;

        public host: IScenePeer;
    }

    class ScenePeer implements IScenePeer {
        public send(route: string, writer: Action<Stream>, priority: PacketPriority, reliability: PacketReliability): void {
        }

        public id: number;
    }

    class BPSLimitationType {
    }
}

interface JQueryStatic {
    stormancer: (configuration: Stormancer.Configuration) => Stormancer.IClient;
}

(function ($, window) {
    Stormancer.jQueryWrapper.initWrapper($);
    //$.stormancer = (configuration: Stormancer.Configuration) => { return Stormancer.Client.CreateClient(configuration); };
    //jQuery.support.cors = true
} (jQuery, window));
