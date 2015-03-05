/// <reference path="Scripts/typings/msgpack/msgpack.d.ts" />
// Module
var Stormancer;
(function (Stormancer) {
    var jQueryWrapper = (function () {
        function jQueryWrapper() {
        }
        jQueryWrapper.initWrapper = function (jquery) {
            jQueryWrapper.$ = jquery;
        };
        return jQueryWrapper;
    })();
    Stormancer.jQueryWrapper = jQueryWrapper;
    // Available packet priorities
    (function (PacketPriority) {
        // The packet is sent immediately without aggregation.
        PacketPriority[PacketPriority["IMMEDIATE_PRIORITY"] = 0] = "IMMEDIATE_PRIORITY";
        // The packet is sent at high priority level.
        PacketPriority[PacketPriority["HIGH_PRIORITY"] = 1] = "HIGH_PRIORITY";
        // The packet is sent at medium priority level.
        PacketPriority[PacketPriority["MEDIUM_PRIORITY"] = 2] = "MEDIUM_PRIORITY";
        // The packet is sent at low priority level.
        PacketPriority[PacketPriority["LOW_PRIORITY"] = 3] = "LOW_PRIORITY";
    })(Stormancer.PacketPriority || (Stormancer.PacketPriority = {}));
    var PacketPriority = Stormancer.PacketPriority;
    /// Different available reliability levels when sending a packet.
    (function (PacketReliability) {
        /// The packet may be lost, or arrive out of order. There are no guarantees whatsoever.
        PacketReliability[PacketReliability["UNRELIABLE"] = 0] = "UNRELIABLE";
        /// The packets arrive in order, but may be lost. If a packet arrives out of order, it is discarded.
        /// The last packet may also never arrive.
        PacketReliability[PacketReliability["UNRELIABLE_SEQUENCED"] = 1] = "UNRELIABLE_SEQUENCED";
        /// The packets always reach destination, but may do so out of order.
        PacketReliability[PacketReliability["RELIABLE"] = 2] = "RELIABLE";
        /// The packets always reach destination and in order.
        PacketReliability[PacketReliability["RELIABLE_ORDERED"] = 3] = "RELIABLE_ORDERED";
        /// The packets arrive at destination in order. If a packet arrive out of order, it is ignored.
        /// That mean that packets may disappear, but the last one always reach destination.
        PacketReliability[PacketReliability["RELIABLE_SEQUENCED"] = 4] = "RELIABLE_SEQUENCED";
    })(Stormancer.PacketReliability || (Stormancer.PacketReliability = {}));
    var PacketReliability = Stormancer.PacketReliability;
    (function (ConnectionState) {
        ConnectionState[ConnectionState["Disconnected"] = 0] = "Disconnected";
        ConnectionState[ConnectionState["Connecting"] = 1] = "Connecting";
        ConnectionState[ConnectionState["Connected"] = 2] = "Connected";
    })(Stormancer.ConnectionState || (Stormancer.ConnectionState = {}));
    var ConnectionState = Stormancer.ConnectionState;
    // Represents the configuration of a Stormancer client.
    var Configuration = (function () {
        function Configuration() {
            //this.dispatcher = new DefaultPacketDispatcher();
            //this.transport = new RaknetTransport(NullLogger.Instance);
            this.serializers = [];
            //this.serializers.push( new MsgPackSerializer() );
        }
        Configuration.prototype.getApiEndpoint = function () {
            if (this.isLocalDev) {
                return this.serverEndpoint ? this.serverEndpoint : Configuration.localDevEndpoint;
            }
            else {
                return this.serverEndpoint ? this.serverEndpoint : Configuration.apiEndpoint;
            }
        };
        // Creates a ClientConfiguration object targeting the local development server.
        Configuration.forLocalDev = function (applicationName) {
            var config = new Configuration();
            config.isLocalDev = true;
            config.application = applicationName;
            config.account = "local";
            return config;
        };
        // Creates a ClientConfiguration object targeting the public online platform.
        Configuration.forAccount = function (accountId, applicationName) {
            var config = new Configuration();
            config.isLocalDev = false;
            config.account = accountId;
            config.application = applicationName;
            return config;
        };
        Configuration.apiEndpoint = "http://api.stormancer.com/";
        Configuration.localDevEndpoint = "http://localhost:8081/";
        return Configuration;
    })();
    Stormancer.Configuration = Configuration;
    var Packet = (function () {
        function Packet(source, data, metadata) {
            this.source = source;
            this.data = data;
            this.metadata = metadata;
        }
        Packet.prototype.getMetadata = function (key) {
            return this.metadata[key];
        };
        return Packet;
    })();
    // Contains method to register handlers for message types when passed to the IPacketProcessor.RegisterProcessor method.
    var PacketProcessorConfig = (function () {
        function PacketProcessorConfig(handlers, defaultprocessors) {
            this._handlers = handlers;
            this._defaultProcessors = defaultprocessors;
        }
        // Adds an handler for the specified message type.
        PacketProcessorConfig.prototype.AddProcessor = function (msgId, handler) {
            if (this._handlers[msgId]) {
            }
            this._handlers[msgId] = handler;
        };
        // Adds
        PacketProcessorConfig.prototype.AddCatchAllProcessor = function (handler) {
            this._defaultProcessors.push(handler);
        };
        return PacketProcessorConfig;
    })();
    var CancellationToken = (function () {
        function CancellationToken() {
        }
        return CancellationToken;
    })();
    var DateTime = (function () {
        function DateTime() {
        }
        return DateTime;
    })();
    var Client = (function () {
        function Client() {
        }
        Client.prototype.getPublicScene = function (sceneId, userData) {
            return;
        };
        Client.prototype.getScene = function (token) {
            return;
        };
        Client.prototype.disconnect = function () {
        };
        Client.prototype.getServerConnectionStatistics = function () {
            return;
        };
        return Client;
    })();
    Stormancer.Client = Client;
    var Scene = (function () {
        function Scene() {
        }
        Scene.prototype.getHostMetadata = function (key) {
            return;
        };
        Scene.prototype.addRoute = function (route, handler, metadata) {
        };
        Scene.prototype.sendPacket = function (route, writer, priority, reliability, channel) {
        };
        return Scene;
    })();
    Stormancer.Scene = Scene;
    var ScenePeer = (function () {
        function ScenePeer() {
        }
        ScenePeer.prototype.send = function (route, writer, priority, reliability) {
        };
        return ScenePeer;
    })();
    var BPSLimitationType = (function () {
        function BPSLimitationType() {
        }
        return BPSLimitationType;
    })();
})(Stormancer || (Stormancer = {}));
(function ($, window) {
    Stormancer.jQueryWrapper.initWrapper($);
    //$.stormancer = (configuration: Stormancer.Configuration) => { return Stormancer.Client.CreateClient(configuration); };
    //jQuery.support.cors = true
}(jQuery, window));
//# sourceMappingURL=stormancer.js.map