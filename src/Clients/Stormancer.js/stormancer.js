var Stormancer;
(function (Stormancer) {
    var ApiClient = (function () {
        function ApiClient(config, tokenHandler) {
            this.createTokenUri = "/{0}/{1}/scenes/{2}/token";
            this._config = config;
            this._tokenHandler = tokenHandler;
        }
        ApiClient.prototype.getSceneEndpoint = function (accountId, applicationName, sceneId, userData) {
            var _this = this;
            var serializer = new Stormancer.MsgPackSerializer();
            var data = serializer.serialize(userData);
            var url = this._config.getApiEndpoint() + Stormancer.Helpers.stringFormat(this.createTokenUri, accountId, applicationName, sceneId);
            return $.ajax({
                type: "POST",
                url: url,
                contentType: "application/msgpack",
                headers: {
                    "Accept": "application/json",
                    "x-version": "1.0.0"
                },
                data: data
            }).then(function (result) {
                return _this._tokenHandler.decodeToken(result);
            });
        };
        return ApiClient;
    })();
    Stormancer.ApiClient = ApiClient;
})(Stormancer || (Stormancer = {}));
var Greeter = (function () {
    function Greeter(element) {
        this.element = element;
        this.element.innerHTML += "The time is: ";
        this.span = document.createElement('span');
        this.element.appendChild(this.span);
    }
    Greeter.prototype.start = function () {
        var _this = this;
        var config = Stormancer.Configuration.forAccount("e376222c-f57c-6cae-8a4d-98fcca54122e", "test");
        config.serverEndpoint = "http://localhost:8081";
        var client = $.stormancer(config);
        var scenePromise = client.getPublicScene("scene1", "antlafarge");
        var deferred = $.Deferred();
        scenePromise.then(function (scene) {
            _this.scene = scene;
            scene.addRoute("echo.out", function (packet) {
                console.log(packet.data[0]);
                console.log(packet.data.length);
            });
            return scene.connect().then(function () {
                _this.timerToken = setInterval(function () {
                    var localDateString = new Date().toLocaleString();
                    _this.span.innerHTML = localDateString;
                    _this.send(localDateString);
                }, 500);
            });
        });
    };
    Greeter.prototype.send = function (message) {
        this.scene.sendPacket("echo.in", new Uint8Array(message.split('')));
    };
    Greeter.prototype.stop = function () {
        clearTimeout(this.timerToken);
    };
    return Greeter;
})();
window.onload = function () {
    var el = document.getElementById('content');
    var greeter = new Greeter(el);
    greeter.start();
};
var Cancellation;
(function (Cancellation) {
    var tokenSource = (function () {
        function tokenSource() {
            this.data = {
                reason: null,
                isCancelled: false,
                listeners: []
            };
            this.token = new token(this.data);
        }
        tokenSource.prototype.cancel = function (reason) {
            this.data.isCancelled = true;
            reason = reason || 'Operation Cancelled';
            this.data.reason = reason;
            setTimeout(function () {
                for (var i = 0; i < this.data.listeners.length; i++) {
                    if (typeof this.data.listeners[i] === 'function') {
                        this.data.listeners[i](reason);
                    }
                }
            }, 0);
        };
        return tokenSource;
    })();
    Cancellation.tokenSource = tokenSource;
    var token = (function () {
        function token(data) {
            this.data = data;
        }
        token.prototype.isCancelled = function () {
            return this.data.isCancelled;
        };
        token.prototype.throwIfCancelled = function () {
            if (this.isCancelled()) {
                throw this.data.reason;
            }
        };
        token.prototype.onCancelled = function (callBack) {
            if (this.isCancelled()) {
                setTimeout(function () {
                    callBack(this.data.reason);
                }, 0);
            }
            else {
                this.data.listeners.push(callBack);
            }
        };
        return token;
    })();
    Cancellation.token = token;
})(Cancellation || (Cancellation = {}));
var Stormancer;
(function (Stormancer) {
    var ConnectionHandler = (function () {
        function ConnectionHandler() {
            this._current = 0;
        }
        ConnectionHandler.prototype.generateNewConnectionId = function () {
            return this._current++;
        };
        ConnectionHandler.prototype.newConnection = function (connection) {
        };
        ConnectionHandler.prototype.getConnection = function (id) {
            throw new Error("Not implemented.");
        };
        ConnectionHandler.prototype.closeConnection = function (connection, reason) {
        };
        return ConnectionHandler;
    })();
    Stormancer.ConnectionHandler = ConnectionHandler;
    var Client = (function () {
        function Client(config) {
            this._tokenHandler = new Stormancer.TokenHandler();
            this._serializers = { msgpack: new Stormancer.MsgPackSerializer() };
            this._systemSerializer = new Stormancer.MsgPackSerializer();
            this._accountId = config.account;
            this._applicationName = config.application;
            this._apiClient = new Stormancer.ApiClient(config, this._tokenHandler);
            this._transport = config.transport;
            this._dispatcher = config.dispatcher;
            this._requestProcessor = new Stormancer.RequestProcessor(this._logger, []);
            this._scenesDispatcher = new Stormancer.SceneDispatcher();
            this._dispatcher.addProcessor(this._requestProcessor);
            this._dispatcher.addProcessor(this._scenesDispatcher);
            this._metadata = config.metadata;
            for (var i in config.serializers) {
                var serializer = config.serializers[i];
                this._serializers[serializer.name] = serializer;
            }
            this._metadata["serializers"] = Stormancer.Helpers.mapKeys(this._serializers).join(',');
            this._metadata["transport"] = this._transport.name;
            this._metadata["version"] = "1.0.0a";
            this._metadata["platform"] = "JS";
            this.initialize();
        }
        Client.prototype.initialize = function () {
            var _this = this;
            if (!this._initialized) {
                this._initialized = true;
                this._transport.packetReceived.push(function (packet) { return _this.transportPacketReceived(packet); });
            }
        };
        Client.prototype.transportPacketReceived = function (packet) {
            this._dispatcher.dispatchPacket(packet);
        };
        Client.prototype.getPublicScene = function (sceneId, userData) {
            var _this = this;
            return this._apiClient.getSceneEndpoint(this._accountId, this._applicationName, sceneId, userData).then(function (ci) { return _this.getSceneImpl(sceneId, ci); });
        };
        Client.prototype.getScene = function (token) {
            var ci = this._tokenHandler.decodeToken(token);
            return this.getSceneImpl(ci.tokenData[7], ci);
        };
        Client.prototype.getSceneImpl = function (sceneId, ci) {
            var self = this;
            return this.ensureTransportStarted(ci).then(function () {
                var parameter = { Metadata: self._serverConnection.metadata, Token: ci.token };
                return self.sendSystemRequest(Stormancer.MessageIDTypes.ID_GET_SCENE_INFOS, parameter);
            }).then(function (result) {
                if (!self._serverConnection.serializer) {
                    if (!result.SelectedSerializer) {
                        throw new Error("No serializer selected.");
                    }
                    self._serverConnection.serializer = self._serializers[result.SelectedSerializer];
                    self._serverConnection.metadata["serializer"] = result.SelectedSerializer;
                }
                var scene = new Stormancer.Scene(self._serverConnection, self, sceneId, ci.token, result);
                return scene;
            });
        };
        Client.prototype.sendSystemRequest = function (id, parameter) {
            var _this = this;
            return this._requestProcessor.sendSystemRequest(this._serverConnection, id, this._systemSerializer.serialize(parameter)).then(function (packet) { return _this._systemSerializer.deserialize(packet.data); });
        };
        Client.prototype.ensureTransportStarted = function (ci) {
            var self = this;
            return Stormancer.Helpers.promiseIf(self._serverConnection == null, function () {
                return Stormancer.Helpers.promiseIf(!self._transport.isRunning, self.startTransport, self).then(function () {
                    return self._transport.connect(ci.tokenData[3][self._transport.name]).then(function (c) { return self.registerConnection(c); });
                });
            }, self);
        };
        Client.prototype.startTransport = function () {
            this._cts = new Cancellation.tokenSource();
            return this._transport.start("client", new ConnectionHandler(), this._cts.token);
        };
        Client.prototype.registerConnection = function (connection) {
            this._serverConnection = connection;
            for (var key in this._metadata) {
                this._serverConnection.metadata[key] = this._metadata[key];
            }
        };
        Client.prototype.disconnectScene = function (scene, sceneHandle) {
            var _this = this;
            return this.sendSystemRequest(Stormancer.MessageIDTypes.ID_DISCONNECT_FROM_SCENE, sceneHandle).then(function () { return _this._scenesDispatcher.removeScene(sceneHandle); });
        };
        Client.prototype.disconnect = function () {
            if (this._serverConnection) {
                this._serverConnection.close();
            }
        };
        Client.prototype.connectToScene = function (scene, token, localRoutes) {
            var _this = this;
            var parameter = {
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
            return this.sendSystemRequest(Stormancer.MessageIDTypes.ID_CONNECT_TO_SCENE, parameter).then(function (result) {
                scene.completeConnectionInitialization(result);
                _this._scenesDispatcher.addScene(scene);
            });
        };
        return Client;
    })();
    Stormancer.Client = Client;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var Configuration = (function () {
        function Configuration() {
            this.metadata = {};
            this.transport = new Stormancer.WebSocketTransport();
            this.dispatcher = new Stormancer.DefaultPacketDispatcher();
            this.serializers = [];
            this.serializers.push(new Stormancer.MsgPackSerializer());
        }
        Configuration.prototype.getApiEndpoint = function () {
            return this.serverEndpoint ? this.serverEndpoint : Configuration.apiEndpoint;
        };
        Configuration.forAccount = function (accountId, applicationName) {
            var config = new Configuration();
            config.account = accountId;
            config.application = applicationName;
            return config;
        };
        Configuration.prototype.Metadata = function (key, value) {
            this.metadata[key] = value;
            return this;
        };
        Configuration.apiEndpoint = "http://api.stormancer.com/";
        return Configuration;
    })();
    Stormancer.Configuration = Configuration;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    (function (ConnectionState) {
        ConnectionState[ConnectionState["Disconnected"] = 0] = "Disconnected";
        ConnectionState[ConnectionState["Connecting"] = 1] = "Connecting";
        ConnectionState[ConnectionState["Connected"] = 2] = "Connected";
    })(Stormancer.ConnectionState || (Stormancer.ConnectionState = {}));
    var ConnectionState = Stormancer.ConnectionState;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var Packet = (function () {
        function Packet(source, data, metadata) {
            this.connection = source;
            this.data = data;
            this._metadata = metadata;
        }
        Packet.prototype.setMetadata = function (metadata) {
            this._metadata = metadata;
        };
        Packet.prototype.getMetadata = function () {
            if (!this._metadata) {
                this._metadata = {};
            }
            return this._metadata;
        };
        Packet.prototype.setMetadataValue = function (key, value) {
            if (!this._metadata) {
                this._metadata = {};
            }
            this._metadata[key] = value;
        };
        Packet.prototype.getMetadataValue = function (key) {
            if (!this._metadata) {
                this._metadata = {};
            }
            return this._metadata[key];
        };
        return Packet;
    })();
    Stormancer.Packet = Packet;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    (function (PacketPriority) {
        PacketPriority[PacketPriority["IMMEDIATE_PRIORITY"] = 0] = "IMMEDIATE_PRIORITY";
        PacketPriority[PacketPriority["HIGH_PRIORITY"] = 1] = "HIGH_PRIORITY";
        PacketPriority[PacketPriority["MEDIUM_PRIORITY"] = 2] = "MEDIUM_PRIORITY";
        PacketPriority[PacketPriority["LOW_PRIORITY"] = 3] = "LOW_PRIORITY";
    })(Stormancer.PacketPriority || (Stormancer.PacketPriority = {}));
    var PacketPriority = Stormancer.PacketPriority;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    (function (PacketReliability) {
        PacketReliability[PacketReliability["UNRELIABLE"] = 0] = "UNRELIABLE";
        PacketReliability[PacketReliability["UNRELIABLE_SEQUENCED"] = 1] = "UNRELIABLE_SEQUENCED";
        PacketReliability[PacketReliability["RELIABLE"] = 2] = "RELIABLE";
        PacketReliability[PacketReliability["RELIABLE_ORDERED"] = 3] = "RELIABLE_ORDERED";
        PacketReliability[PacketReliability["RELIABLE_SEQUENCED"] = 4] = "RELIABLE_SEQUENCED";
    })(Stormancer.PacketReliability || (Stormancer.PacketReliability = {}));
    var PacketReliability = Stormancer.PacketReliability;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var Route = (function () {
        function Route(scene, name, index, metadata) {
            if (index === void 0) { index = 0; }
            if (metadata === void 0) { metadata = {}; }
            this.scene = scene;
            this.name = name;
            this.index = index;
            this.metadata = metadata;
            this.handlers = [];
        }
        return Route;
    })();
    Stormancer.Route = Route;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var Helpers = (function () {
        function Helpers() {
        }
        Helpers.base64ToByteArray = function (data) {
            return new Uint8Array(atob(data).split('').map(function (c) {
                return c.charCodeAt(0);
            }));
        };
        Helpers.stringFormat = function (str) {
            var args = [];
            for (var _i = 1; _i < arguments.length; _i++) {
                args[_i - 1] = arguments[_i];
            }
            for (var i in args) {
                str = str.replace('{' + i + '}', args[i]);
            }
            return str;
        };
        Helpers.mapKeys = function (map) {
            var keys = [];
            for (var key in map) {
                if (map.hasOwnProperty(key)) {
                    keys.push(key);
                }
            }
            return keys;
        };
        Helpers.mapValues = function (map) {
            var result = [];
            for (var key in map) {
                result.push(map[key]);
            }
            return result;
        };
        Helpers.promiseFromResult = function (result) {
            var deferred = jQuery.Deferred();
            deferred.resolve(result);
            return deferred.promise();
        };
        Helpers.promiseIf = function (condition, action, context) {
            if (condition) {
                if (context) {
                    return action.call(context);
                }
                else {
                    return action();
                }
            }
            else {
                return Helpers.promiseFromResult(null);
            }
        };
        return Helpers;
    })();
    Stormancer.Helpers = Helpers;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var DefaultPacketDispatcher = (function () {
        function DefaultPacketDispatcher() {
            this._handlers = {};
            this._defaultProcessors = [];
        }
        DefaultPacketDispatcher.prototype.dispatchPacket = function (packet) {
            var processed = false;
            var count = 0;
            var msgType = 0;
            while (!processed && count < 40) {
                msgType = packet.data[0];
                packet.data = packet.data.subarray(1);
                if (this._handlers[msgType]) {
                    processed = this._handlers[msgType](packet);
                    count++;
                }
                else {
                    break;
                }
            }
            for (var i = 0, len = this._defaultProcessors.length; i < len; i++) {
                if (this._defaultProcessors[i](msgType, packet)) {
                    processed = true;
                    break;
                }
            }
            if (!processed) {
                throw new Error("Couldn't process message. msgId: " + msgType);
            }
        };
        DefaultPacketDispatcher.prototype.addProcessor = function (processor) {
            processor.registerProcessor(new Stormancer.PacketProcessorConfig(this._handlers, this._defaultProcessors));
        };
        return DefaultPacketDispatcher;
    })();
    Stormancer.DefaultPacketDispatcher = DefaultPacketDispatcher;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var TokenHandler = (function () {
        function TokenHandler() {
            this._tokenSerializer = new Stormancer.MsgPackSerializer();
        }
        TokenHandler.prototype.decodeToken = function (token) {
            var data = token.split('-')[0];
            var buffer = Stormancer.Helpers.base64ToByteArray(data);
            var result = this._tokenSerializer.deserialize(buffer);
            var sceneEndpoint = new Stormancer.SceneEndpoint();
            sceneEndpoint.token = token;
            sceneEndpoint.tokenData = result;
            return sceneEndpoint;
        };
        return TokenHandler;
    })();
    Stormancer.TokenHandler = TokenHandler;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var MsgPackSerializer = (function () {
        function MsgPackSerializer() {
            this.name = "msgpack/map";
        }
        MsgPackSerializer.prototype.serialize = function (data) {
            return new Uint8Array(msgpack.pack(data));
        };
        MsgPackSerializer.prototype.deserialize = function (bytes) {
            return msgpack.unpack(bytes);
        };
        return MsgPackSerializer;
    })();
    Stormancer.MsgPackSerializer = MsgPackSerializer;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var PacketProcessorConfig = (function () {
        function PacketProcessorConfig(handlers, defaultprocessors) {
            this._handlers = handlers;
            this._defaultProcessors = defaultprocessors;
        }
        PacketProcessorConfig.prototype.addProcessor = function (msgId, handler) {
            if (this._handlers[msgId]) {
                throw new Error("An handler is already registered for id " + msgId);
            }
            this._handlers[msgId] = handler;
        };
        PacketProcessorConfig.prototype.addCatchAllProcessor = function (handler) {
            this._defaultProcessors.push(function (n, p) { return handler(n, p); });
        };
        return PacketProcessorConfig;
    })();
    Stormancer.PacketProcessorConfig = PacketProcessorConfig;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var MessageIDTypes = (function () {
        function MessageIDTypes() {
        }
        MessageIDTypes.ID_CONNECT_TO_SCENE = 134;
        MessageIDTypes.ID_DISCONNECT_FROM_SCENE = 135;
        MessageIDTypes.ID_GET_SCENE_INFOS = 136;
        MessageIDTypes.ID_REQUEST_RESPONSE_MSG = 137;
        MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE = 138;
        MessageIDTypes.ID_REQUEST_RESPONSE_ERROR = 139;
        MessageIDTypes.ID_CONNECTION_RESULT = 140;
        MessageIDTypes.ID_SCENES = 141;
        return MessageIDTypes;
    })();
    Stormancer.MessageIDTypes = MessageIDTypes;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var RequestContext = (function () {
        function RequestContext(p) {
            this._didSendValues = false;
            this.isComplete = false;
            this._packet = p;
            this._requestId = p.data.subarray(0, 2);
            this.inputData = p.data.subarray(2);
        }
        RequestContext.prototype.send = function (data) {
            if (this.isComplete) {
                throw new Error("The request is already completed.");
            }
            this._didSendValues = true;
            var dataToSend = new Uint8Array(2 + data.length);
            dataToSend.set(this._requestId);
            dataToSend.set(data, 2);
            this._packet.connection.sendSystem(Stormancer.MessageIDTypes.ID_REQUEST_RESPONSE_MSG, dataToSend);
        };
        RequestContext.prototype.complete = function () {
            var dataToSend = new Uint8Array(3);
            dataToSend.set(this._requestId);
            dataToSend.set(2, this._didSendValues ? 1 : 0);
            this._packet.connection.sendSystem(Stormancer.MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE, dataToSend);
        };
        RequestContext.prototype.error = function (data) {
            var dataToSend = new Uint8Array(2 + data.length);
            dataToSend.set(this._requestId);
            dataToSend.set(data, 2);
            this._packet.connection.sendSystem(Stormancer.MessageIDTypes.ID_REQUEST_RESPONSE_ERROR, dataToSend);
        };
        return RequestContext;
    })();
    Stormancer.RequestContext = RequestContext;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var RequestProcessor = (function () {
        function RequestProcessor(logger, modules) {
            this._pendingRequests = {};
            this._isRegistered = false;
            this._handlers = {};
            this._pendingRequests = {};
            this._logger = logger;
            for (var key in modules) {
                var mod = modules[key];
                mod.register(this.addSystemRequestHandler);
            }
        }
        RequestProcessor.prototype.registerProcessor = function (config) {
            var _this = this;
            this._isRegistered = true;
            for (var key in this._handlers) {
                var handler = this._handlers[key];
                config.addProcessor(key, function (p) {
                    var context = new Stormancer.RequestContext(p);
                    var continuation = function (fault) {
                        if (!context.isComplete) {
                            if (fault) {
                                context.error(p.connection.serializer.serialize(fault));
                            }
                            else {
                                context.complete();
                            }
                        }
                    };
                    handler(context).done(function () { return continuation(null); }).fail(function (error) { return continuation(error); });
                    return true;
                });
            }
            config.addProcessor(Stormancer.MessageIDTypes.ID_REQUEST_RESPONSE_MSG, function (p) {
                var id = new DataView(p.data.buffer, p.data.byteOffset).getUint16(0, true);
                var request = _this._pendingRequests[id];
                if (request) {
                    p.setMetadataValue["request"] = request;
                    request.lastRefresh = new Date();
                    p.data = p.data.subarray(2);
                    request.observer.onNext(p);
                    request.deferred.resolve();
                }
                else {
                    console.error("Unknow request id.");
                }
                return true;
            });
            config.addProcessor(Stormancer.MessageIDTypes.ID_REQUEST_RESPONSE_COMPLETE, function (p) {
                var id = new DataView(p.data.buffer, p.data.byteOffset).getUint16(0, true);
                var request = _this._pendingRequests[id];
                if (request) {
                    p.setMetadataValue("request", request);
                }
                else {
                    console.error("Unknow request id.");
                }
                delete _this._pendingRequests[id];
                if (p.data[3]) {
                    request.deferred.promise().always(function () { return request.observer.onCompleted(); });
                }
                else {
                    request.observer.onCompleted();
                }
                return true;
            });
            config.addProcessor(Stormancer.MessageIDTypes.ID_REQUEST_RESPONSE_ERROR, function (p) {
                var id = new DataView(p.data.buffer, p.data.byteOffset).getUint16(0, true);
                var request = _this._pendingRequests[id];
                if (request) {
                    p.setMetadataValue("request", request);
                }
                else {
                    console.error("Unknow request id.");
                }
                delete _this._pendingRequests[id];
                var msg = p.connection.serializer.deserialize(p.data.subarray(2));
                request.observer.onError(new Error(msg));
                return true;
            });
        };
        RequestProcessor.prototype.addSystemRequestHandler = function (msgId, handler) {
            if (this._isRegistered) {
                throw new Error("Can only add handler before 'registerProcessor' is called.");
            }
            this._handlers[msgId] = handler;
        };
        RequestProcessor.prototype.reserveRequestSlot = function (observer) {
            var id = 0;
            while (id < 65535) {
                if (!this._pendingRequests[id]) {
                    var request = { lastRefresh: new Date, id: id, observer: observer, deferred: jQuery.Deferred() };
                    this._pendingRequests[id] = request;
                    return request;
                }
                id++;
            }
            throw new Error("Unable to create new request: Too many pending requests.");
        };
        RequestProcessor.prototype.sendSystemRequest = function (peer, msgId, data) {
            var _this = this;
            var deferred = $.Deferred();
            var request = this.reserveRequestSlot({
                onNext: function (packet) {
                    deferred.resolve(packet);
                },
                onError: function (e) {
                    deferred.reject(e);
                },
                onCompleted: function () {
                }
            });
            var dataToSend = new Uint8Array(2 + data.length);
            var idArray = new Uint16Array([request.id]);
            dataToSend.set(new Uint8Array(idArray.buffer));
            dataToSend.set(data, 2);
            peer.sendSystem(msgId, dataToSend);
            deferred.promise().always(function () {
                var r = _this._pendingRequests[request.id];
                if (r == request) {
                    delete _this._pendingRequests[request.id];
                }
            });
            return deferred.promise();
        };
        return RequestProcessor;
    })();
    Stormancer.RequestProcessor = RequestProcessor;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var SceneDispatcher = (function () {
        function SceneDispatcher() {
            this._scenes = [];
        }
        SceneDispatcher.prototype.registerProcessor = function (config) {
            config.addCatchAllProcessor(this.handler);
        };
        SceneDispatcher.prototype.handler = function (sceneHandler, packet) {
            if (sceneHandler < Stormancer.MessageIDTypes.ID_SCENES) {
                return false;
            }
            var scene = this._scenes[sceneHandler - Stormancer.MessageIDTypes.ID_SCENES];
            if (!scene) {
                return false;
            }
            else {
                packet.setMetadataValue("scene", scene);
                scene.handleMessage(packet);
                return true;
            }
        };
        SceneDispatcher.prototype.addScene = function (scene) {
            this._scenes[scene.handle - Stormancer.MessageIDTypes.ID_SCENES] = scene;
        };
        SceneDispatcher.prototype.removeScene = function (sceneHandle) {
            delete this._scenes[sceneHandle - Stormancer.MessageIDTypes.ID_SCENES];
        };
        return SceneDispatcher;
    })();
    Stormancer.SceneDispatcher = SceneDispatcher;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var Scene = (function () {
        function Scene(connection, client, id, token, dto) {
            this._remoteRoutesMap = {};
            this._localRoutesMap = {};
            this._handlers = {};
            this.id = id;
            this.hostConnection = connection;
            this._token = token;
            this._client = client;
            this._metadata = dto.Metadata;
            for (var i = 0; i < dto.Routes.length; i++) {
                var route = dto.Routes[i];
                this._remoteRoutesMap[route.Name] = new Stormancer.Route(this, route.Name, route.Handle, route.Metadata);
            }
        }
        Scene.prototype.getHostMetadata = function (key) {
            return this._metadata[key];
        };
        Scene.prototype.addRoute = function (route, handler, metadata) {
            if (metadata === void 0) { metadata = {}; }
            if (route[0] === "@") {
                throw new Error("A route cannot start with the @ character.");
            }
            if (this.connected) {
                throw new Error("You cannot register handles once the scene is connected.");
            }
            var routeObj = this._localRoutesMap[route];
            if (!routeObj) {
                routeObj = new Stormancer.Route(this, route, 0, metadata);
                this._localRoutesMap[route] = routeObj;
            }
            this.onMessage(route, handler);
        };
        Scene.prototype.onMessage = function (route, handler) {
            if (this.connected) {
                throw new Error("You cannot register handles once the scene is connected.");
            }
            var routeObj = this._localRoutesMap[route];
            if (!routeObj) {
                routeObj = new Stormancer.Route(this, route);
                this._localRoutesMap[route] = routeObj;
            }
            this.onMessageImpl(routeObj, handler);
        };
        Scene.prototype.onMessageImpl = function (route, handler) {
            var _this = this;
            var index = route.index;
            var action = function (p) {
                var packet = new Stormancer.Packet(_this.host(), p.data, p.getMetadata());
                handler(packet);
            };
            route.handlers.push(function (p) { return action(p); });
        };
        Scene.prototype.sendPacket = function (route, data, priority, reliability) {
            if (priority === void 0) { priority = 2 /* MEDIUM_PRIORITY */; }
            if (reliability === void 0) { reliability = 2 /* RELIABLE */; }
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
        };
        Scene.prototype.connect = function () {
            var _this = this;
            return this._client.connectToScene(this, this._token, Stormancer.Helpers.mapValues(this._localRoutesMap)).then(function () {
                _this.connected = true;
            });
        };
        Scene.prototype.disconnect = function () {
            return this._client.disconnectScene(this, this.handle);
        };
        Scene.prototype.handleMessage = function (packet) {
            var ev = this.packetReceived;
            ev && ev.map(function (value) {
                value(packet);
            });
            var routeId = new Uint16Array(packet.data.buffer, packet.data.byteOffset, 2)[0];
            packet.setMetadataValue("routeId", routeId);
            var observer = this._handlers[routeId];
            observer && observer.map(function (value) {
                value(packet);
            });
        };
        Scene.prototype.completeConnectionInitialization = function (cr) {
            this.handle = cr.SceneHandle;
            for (var key in this._localRoutesMap) {
                var route = this._localRoutesMap[key];
                route.index = cr.RouteMappings[key];
                this._handlers[route.index] = route.handlers;
            }
        };
        Scene.prototype.host = function () {
            return new Stormancer.ScenePeer(this.hostConnection, this.handle, this._remoteRoutesMap, this);
        };
        return Scene;
    })();
    Stormancer.Scene = Scene;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var SceneEndpoint = (function () {
        function SceneEndpoint() {
        }
        return SceneEndpoint;
    })();
    Stormancer.SceneEndpoint = SceneEndpoint;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var ScenePeer = (function () {
        function ScenePeer(connection, sceneHandle, routeMapping, scene) {
            this._connection = connection;
            this._sceneHandle = sceneHandle;
            this._routeMapping = routeMapping;
            this._scene = scene;
        }
        ScenePeer.prototype.id = function () {
            return this._connection.id;
        };
        ScenePeer.prototype.send = function (route, data, priority, reliability) {
            var r = this._routeMapping[route];
            if (!r) {
                throw new Error("The route " + route + " is not declared on the server.");
            }
            this._connection.sendToScene(this._sceneHandle, r.index, data, priority, reliability);
        };
        return ScenePeer;
    })();
    Stormancer.ScenePeer = ScenePeer;
})(Stormancer || (Stormancer = {}));
this.msgpack || (function (globalScope) {
    globalScope.msgpack = {
        pack: msgpackpack,
        unpack: msgpackunpack,
        worker: "msgpack.js",
        upload: msgpackupload,
        download: msgpackdownload
    };
    var _ie = /MSIE/.test(navigator.userAgent), _bin2num = {}, _num2bin = {}, _num2b64 = ("ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "abcdefghijklmnopqrstuvwxyz0123456789+/").split(""), _buf = [], _idx = 0, _error = 0, _isArray = Array.isArray || (function (mix) {
        return Object.prototype.toString.call(mix) === "[object Array]";
    }), _toString = String.fromCharCode, _MAX_DEPTH = 512;
    self.importScripts && (onmessage = function (event) {
        if (event.data.method === "pack") {
            window.postMessage(base64encode(msgpackpack(event.data.data)));
        }
        else {
            window.postMessage(msgpackunpack(event.data.data));
        }
    });
    function msgpackpack(data, settings) {
        var toString = false;
        _error = 0;
        if (!settings) {
            settings = { byteProperties: [] };
        }
        var byteArray = encode([], data, 0, settings);
        return _error ? false : toString ? byteArrayToByteString(byteArray) : byteArray;
    }
    function msgpackunpack(data, settings) {
        if (!settings) {
            settings = { byteProperties: [] };
        }
        _buf = typeof data === "string" ? toByteArray(data) : data;
        _idx = -1;
        return decode(settings);
    }
    function encode(rv, mix, depth, settings, bytesArray) {
        var size, i, iz, c, pos, high, low, sign, exp, frac;
        if (mix == null) {
            rv.push(0xc0);
        }
        else if (mix === false) {
            rv.push(0xc2);
        }
        else if (mix === true) {
            rv.push(0xc3);
        }
        else {
            switch (typeof mix) {
                case "number":
                    if (mix !== mix) {
                        rv.push(0xcb, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff);
                    }
                    else if (mix === Infinity) {
                        rv.push(0xcb, 0x7f, 0xf0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
                    }
                    else if (Math.floor(mix) === mix) {
                        if (mix < 0) {
                            if (mix >= -32) {
                                rv.push(0xe0 + mix + 32);
                            }
                            else if (mix > -0x80) {
                                rv.push(0xd0, mix + 0x100);
                            }
                            else if (mix > -0x8000) {
                                mix += 0x10000;
                                rv.push(0xd1, mix >> 8, mix & 0xff);
                            }
                            else if (mix > -0x80000000) {
                                mix += 0x100000000;
                                rv.push(0xd2, mix >>> 24, (mix >> 16) & 0xff, (mix >> 8) & 0xff, mix & 0xff);
                            }
                            else {
                                high = Math.floor(mix / 0x100000000);
                                low = mix & 0xffffffff;
                                rv.push(0xd3, (high >> 24) & 0xff, (high >> 16) & 0xff, (high >> 8) & 0xff, high & 0xff, (low >> 24) & 0xff, (low >> 16) & 0xff, (low >> 8) & 0xff, low & 0xff);
                            }
                        }
                        else {
                            if (mix < 0x80) {
                                rv.push(mix);
                            }
                            else if (mix < 0x100) {
                                rv.push(0xcc, mix);
                            }
                            else if (mix < 0x10000) {
                                rv.push(0xcd, mix >> 8, mix & 0xff);
                            }
                            else if (mix < 0x100000000) {
                                rv.push(0xce, mix >>> 24, (mix >> 16) & 0xff, (mix >> 8) & 0xff, mix & 0xff);
                            }
                            else {
                                high = Math.floor(mix / 0x100000000);
                                low = mix & 0xffffffff;
                                rv.push(0xcf, (high >> 24) & 0xff, (high >> 16) & 0xff, (high >> 8) & 0xff, high & 0xff, (low >> 24) & 0xff, (low >> 16) & 0xff, (low >> 8) & 0xff, low & 0xff);
                            }
                        }
                    }
                    else {
                        sign = mix < 0;
                        sign && (mix *= -1);
                        exp = ((Math.log(mix) / 0.6931471805599453) + 1023) | 0;
                        frac = mix * Math.pow(2, 52 + 1023 - exp);
                        low = frac & 0xffffffff;
                        sign && (exp |= 0x800);
                        high = ((frac / 0x100000000) & 0xfffff) | (exp << 20);
                        rv.push(0xcb, (high >> 24) & 0xff, (high >> 16) & 0xff, (high >> 8) & 0xff, high & 0xff, (low >> 24) & 0xff, (low >> 16) & 0xff, (low >> 8) & 0xff, low & 0xff);
                    }
                    break;
                case "string":
                    iz = mix.length;
                    pos = rv.length;
                    rv.push(0);
                    for (i = 0; i < iz; ++i) {
                        c = mix.charCodeAt(i);
                        if (c < 0x80) {
                            rv.push(c & 0x7f);
                        }
                        else if (c < 0x0800) {
                            rv.push(((c >>> 6) & 0x1f) | 0xc0, (c & 0x3f) | 0x80);
                        }
                        else if (c < 0x10000) {
                            rv.push(((c >>> 12) & 0x0f) | 0xe0, ((c >>> 6) & 0x3f) | 0x80, (c & 0x3f) | 0x80);
                        }
                    }
                    size = rv.length - pos - 1;
                    if (size < 32) {
                        rv[pos] = 0xa0 + size;
                    }
                    else if (size < 0x10000) {
                        rv.splice(pos, 1, 0xda, size >> 8, size & 0xff);
                    }
                    else if (size < 0x100000000) {
                        rv.splice(pos, 1, 0xdb, size >>> 24, (size >> 16) & 0xff, (size >> 8) & 0xff, size & 0xff);
                    }
                    break;
                default:
                    if (++depth >= _MAX_DEPTH) {
                        _error = 1;
                        return rv = [];
                    }
                    if (_isArray(mix)) {
                        if (bytesArray) {
                            size = mix.length;
                            if (size < 32) {
                                rv.push(0xa0 + size);
                            }
                            else if (size < 0x10000) {
                                rv.push(0xda, size >> 8, size & 0xff);
                            }
                            else if (size < 0x100000000) {
                                rv.push(0xdb, size >>> 24, (size >> 16) & 0xff, (size >> 8) & 0xff, size & 0xff);
                            }
                            for (i = 0; i < size; ++i) {
                                rv.push(mix[i]);
                            }
                        }
                        else {
                            size = mix.length;
                            if (size < 16) {
                                rv.push(0x90 + size);
                            }
                            else if (size < 0x10000) {
                                rv.push(0xdc, size >> 8, size & 0xff);
                            }
                            else if (size < 0x100000000) {
                                rv.push(0xdd, size >>> 24, (size >> 16) & 0xff, (size >> 8) & 0xff, size & 0xff);
                            }
                            for (i = 0; i < size; ++i) {
                                encode(rv, mix[i], depth, settings);
                            }
                        }
                    }
                    else {
                        pos = rv.length;
                        rv.push(0);
                        size = 0;
                        for (i in mix) {
                            if (typeof (mix[i]) == "function") {
                                continue;
                            }
                            ++size;
                            encode(rv, i, depth);
                            if ($.inArray(i, settings.byteProperties) != -1) {
                                encode(rv, mix[i], depth, settings, true);
                            }
                            else {
                                encode(rv, mix[i], depth, settings, false);
                            }
                        }
                        if (size < 16) {
                            rv[pos] = 0x80 + size;
                        }
                        else if (size < 0x10000) {
                            rv.splice(pos, 1, 0xde, size >> 8, size & 0xff);
                        }
                        else if (size < 0x100000000) {
                            rv.splice(pos, 1, 0xdf, size >>> 24, (size >> 16) & 0xff, (size >> 8) & 0xff, size & 0xff);
                        }
                    }
            }
        }
        return rv;
    }
    function decode(settings, rawAsArray) {
        var size, i, iz, c, num = 0, sign, exp, frac, ary, hash, buf = _buf, type = buf[++_idx], key;
        if (type >= 0xe0) {
            return type - 0x100;
        }
        if (type < 0xc0) {
            if (type < 0x80) {
                return type;
            }
            if (type < 0x90) {
                num = type - 0x80;
                type = 0x80;
            }
            else if (type < 0xa0) {
                num = type - 0x90;
                type = 0x90;
            }
            else {
                num = type - 0xa0;
                type = 0xa0;
            }
        }
        switch (type) {
            case 0xc0: return null;
            case 0xc2: return false;
            case 0xc3: return true;
            case 0xca:
                num = buf[++_idx] * 0x1000000 + (buf[++_idx] << 16) + (buf[++_idx] << 8) + buf[++_idx];
                sign = num & 0x80000000;
                exp = (num >> 23) & 0xff;
                frac = num & 0x7fffff;
                if (!num || num === 0x80000000) {
                    return 0;
                }
                if (exp === 0xff) {
                    return frac ? NaN : Infinity;
                }
                return (sign ? -1 : 1) * (frac | 0x800000) * Math.pow(2, exp - 127 - 23);
            case 0xcb:
                num = buf[++_idx] * 0x1000000 + (buf[++_idx] << 16) + (buf[++_idx] << 8) + buf[++_idx];
                sign = num & 0x80000000;
                exp = (num >> 20) & 0x7ff;
                frac = num & 0xfffff;
                if (!num || num === 0x80000000) {
                    _idx += 4;
                    return 0;
                }
                if (exp === 0x7ff) {
                    _idx += 4;
                    return frac ? NaN : Infinity;
                }
                num = buf[++_idx] * 0x1000000 + (buf[++_idx] << 16) + (buf[++_idx] << 8) + buf[++_idx];
                return (sign ? -1 : 1) * ((frac | 0x100000) * Math.pow(2, exp - 1023 - 20) + num * Math.pow(2, exp - 1023 - 52));
            case 0xcf:
                num = buf[++_idx] * 0x1000000 + (buf[++_idx] << 16) + (buf[++_idx] << 8) + buf[++_idx];
                return num * 0x100000000 + buf[++_idx] * 0x1000000 + (buf[++_idx] << 16) + (buf[++_idx] << 8) + buf[++_idx];
            case 0xce: num += buf[++_idx] * 0x1000000 + (buf[++_idx] << 16);
            case 0xcd: num += buf[++_idx] << 8;
            case 0xcc: return num + buf[++_idx];
            case 0xd3:
                num = buf[++_idx];
                if (num & 0x80) {
                    return ((num ^ 0xff) * 0x100000000000000 + (buf[++_idx] ^ 0xff) * 0x1000000000000 + (buf[++_idx] ^ 0xff) * 0x10000000000 + (buf[++_idx] ^ 0xff) * 0x100000000 + (buf[++_idx] ^ 0xff) * 0x1000000 + (buf[++_idx] ^ 0xff) * 0x10000 + (buf[++_idx] ^ 0xff) * 0x100 + (buf[++_idx] ^ 0xff) + 1) * -1;
                }
                return num * 0x100000000000000 + buf[++_idx] * 0x1000000000000 + buf[++_idx] * 0x10000000000 + buf[++_idx] * 0x100000000 + buf[++_idx] * 0x1000000 + buf[++_idx] * 0x10000 + buf[++_idx] * 0x100 + buf[++_idx];
            case 0xd2:
                num = buf[++_idx] * 0x1000000 + (buf[++_idx] << 16) + (buf[++_idx] << 8) + buf[++_idx];
                return num < 0x80000000 ? num : num - 0x100000000;
            case 0xd1:
                num = (buf[++_idx] << 8) + buf[++_idx];
                return num < 0x8000 ? num : num - 0x10000;
            case 0xd0:
                num = buf[++_idx];
                return num < 0x80 ? num : num - 0x100;
            case 0xdb: num += buf[++_idx] * 0x1000000 + (buf[++_idx] << 16);
            case 0xda: num += (buf[++_idx] << 8) + buf[++_idx];
            case 0xa0:
                if (rawAsArray) {
                    for (ary = [], i = _idx, iz = i + num; i < iz;) {
                        ary.push(buf[++i]);
                    }
                    _idx = i;
                    return ary;
                }
                else {
                    for (ary = [], i = _idx, iz = i + num; i < iz;) {
                        c = buf[++i];
                        ary.push(c < 0x80 ? c : c < 0xe0 ? ((c & 0x1f) << 6 | (buf[++i] & 0x3f)) : ((c & 0x0f) << 12 | (buf[++i] & 0x3f) << 6 | (buf[++i] & 0x3f)));
                    }
                    _idx = i;
                    return ary.length < 10240 ? _toString.apply(null, ary) : byteArrayToByteString(ary);
                }
            case 0xdf: num += buf[++_idx] * 0x1000000 + (buf[++_idx] << 16);
            case 0xde: num += (buf[++_idx] << 8) + buf[++_idx];
            case 0x80:
                hash = {};
                while (num--) {
                    size = buf[++_idx] - 0xa0;
                    for (ary = [], i = _idx, iz = i + size; i < iz;) {
                        c = buf[++i];
                        ary.push(c < 0x80 ? c : c < 0xe0 ? ((c & 0x1f) << 6 | (buf[++i] & 0x3f)) : ((c & 0x0f) << 12 | (buf[++i] & 0x3f) << 6 | (buf[++i] & 0x3f)));
                    }
                    _idx = i;
                    key = _toString.apply(null, ary);
                    if ($.inArray(key, settings.byteProperties) != -1) {
                        hash[key] = decode(settings, true);
                    }
                    else {
                        hash[key] = decode(settings);
                    }
                }
                return hash;
            case 0xdd: num += buf[++_idx] * 0x1000000 + (buf[++_idx] << 16);
            case 0xdc: num += (buf[++_idx] << 8) + buf[++_idx];
            case 0x90:
                ary = [];
                while (num--) {
                    ary.push(decode(settings, rawAsArray));
                }
                return ary;
        }
        return;
    }
    function byteArrayToByteString(byteArray) {
        try {
            return _toString.apply(this, byteArray);
        }
        catch (err) {
            ;
        }
        var rv = [], i = 0, iz = byteArray.length, num2bin = _num2bin;
        for (; i < iz; ++i) {
            rv[i] = num2bin[byteArray[i]];
        }
        return rv.join("");
    }
    function msgpackdownload(url, option, callback) {
        option.method = "GET";
        option.binary = true;
        ajax(url, option, callback);
    }
    function msgpackupload(url, option, callback) {
        option.method = "PUT";
        option.binary = true;
        if (option.worker && globalScope.Worker) {
            var worker = new Worker(globalScope.msgpack.worker);
            worker.onmessage = function (event) {
                option.data = event.data;
                ajax(url, option, callback);
            };
            worker.postMessage({ method: "pack", data: option.data });
        }
        else {
            option.data = base64encode(msgpackpack(option.data));
            ajax(url, option, callback);
        }
    }
    function ajax(url, option, callback) {
        function readyStateChange() {
            if (xhr.readyState === 4) {
                var data, status = xhr.status, worker, byteArray, rv = { status: status, ok: status >= 200 && status < 300 };
                if (!run++) {
                    if (method === "PUT") {
                        data = rv.ok ? xhr.responseText : "";
                    }
                    else {
                        if (rv.ok) {
                            if (option.worker && globalScope.Worker) {
                                worker = new Worker(globalScope.msgpack.worker);
                                worker.onmessage = function (event) {
                                    callback(event.data, option, rv);
                                };
                                worker.postMessage({
                                    method: "unpack",
                                    data: xhr.responseText
                                });
                                gc();
                                return;
                            }
                            else {
                                byteArray = _ie ? toByteArrayIE(xhr) : toByteArray(xhr.responseText);
                                data = msgpackunpack(byteArray);
                            }
                        }
                    }
                    after && after(xhr, option, rv);
                    callback(data, option, rv);
                    gc();
                }
            }
        }
        function ng(abort, status) {
            if (!run++) {
                var rv = { status: status || 400, ok: false };
                after && after(xhr, option, rv);
                callback(null, option, rv);
                gc(abort);
            }
        }
        function gc(abort) {
            abort && xhr && xhr.abort && xhr.abort();
            watchdog && (clearTimeout(watchdog), watchdog = 0);
            xhr = null;
            globalScope.addEventListener && globalScope.removeEventListener("beforeunload", ng, false);
        }
        var watchdog = 0, method = option.method || "GET", header = option.header || {}, before = option.before, after = option.after, data = option.data || null, xhr = globalScope.XMLHttpRequest ? new XMLHttpRequest() : globalScope.ActiveXObject ? new ActiveXObject("Microsoft.XMLHTTP") : null, run = 0, i, overrideMimeType = "overrideMimeType", setRequestHeader = "setRequestHeader", getbinary = method === "GET" && option.binary;
        try {
            xhr.onreadystatechange = readyStateChange;
            xhr.open(method, url, true);
            before && before(xhr, option);
            getbinary && xhr[overrideMimeType] && xhr[overrideMimeType]("text/plain; charset=x-user-defined");
            data && xhr[setRequestHeader]("Content-Type", "application/x-www-form-urlencoded");
            for (i in header) {
                xhr[setRequestHeader](i, header[i]);
            }
            globalScope.addEventListener && globalScope.addEventListener("beforeunload", ng, false);
            xhr.send(data);
            watchdog = setTimeout(function () {
                ng(1, 408);
            }, (option.timeout || 10) * 1000);
        }
        catch (err) {
            ng(0, 400);
        }
    }
    function toByteArray(data) {
        var rv = [], bin2num = _bin2num, remain, ary = data.split(""), i = -1, iz;
        iz = ary.length;
        remain = iz % 8;
        while (remain--) {
            ++i;
            rv[i] = bin2num[ary[i]];
        }
        remain = iz >> 3;
        while (remain--) {
            rv.push(bin2num[ary[++i]], bin2num[ary[++i]], bin2num[ary[++i]], bin2num[ary[++i]], bin2num[ary[++i]], bin2num[ary[++i]], bin2num[ary[++i]], bin2num[ary[++i]]);
        }
        return rv;
    }
    function toByteArrayIE(xhr) {
        var rv = [], data, remain, charCodeAt = "charCodeAt", loop, v0, v1, v2, v3, v4, v5, v6, v7, i = -1, iz;
        iz = vblen(xhr);
        data = vbstr(xhr);
        loop = Math.ceil(iz / 2);
        remain = loop % 8;
        while (remain--) {
            v0 = data[charCodeAt](++i);
            rv.push(v0 & 0xff, v0 >> 8);
        }
        remain = loop >> 3;
        while (remain--) {
            v0 = data[charCodeAt](++i);
            v1 = data[charCodeAt](++i);
            v2 = data[charCodeAt](++i);
            v3 = data[charCodeAt](++i);
            v4 = data[charCodeAt](++i);
            v5 = data[charCodeAt](++i);
            v6 = data[charCodeAt](++i);
            v7 = data[charCodeAt](++i);
            rv.push(v0 & 0xff, v0 >> 8, v1 & 0xff, v1 >> 8, v2 & 0xff, v2 >> 8, v3 & 0xff, v3 >> 8, v4 & 0xff, v4 >> 8, v5 & 0xff, v5 >> 8, v6 & 0xff, v6 >> 8, v7 & 0xff, v7 >> 8);
        }
        iz % 2 && rv.pop();
        return rv;
    }
    function base64encode(data) {
        var rv = [], c = 0, i = -1, iz = data.length, pad = [0, 2, 1][data.length % 3], num2bin = _num2bin, num2b64 = _num2b64;
        if (globalScope.btoa) {
            while (i < iz) {
                rv.push(num2bin[data[++i]]);
            }
            return btoa(rv.join(""));
        }
        --iz;
        while (i < iz) {
            c = (data[++i] << 16) | (data[++i] << 8) | (data[++i]);
            rv.push(num2b64[(c >> 18) & 0x3f], num2b64[(c >> 12) & 0x3f], num2b64[(c >> 6) & 0x3f], num2b64[c & 0x3f]);
        }
        pad > 1 && (rv[rv.length - 2] = "=");
        pad > 0 && (rv[rv.length - 1] = "=");
        return rv.join("");
    }
    (function () {
        var i = 0, v;
        for (; i < 0x100; ++i) {
            v = _toString(i);
            _bin2num[v] = i;
            _num2bin[i] = v;
        }
        for (i = 0x80; i < 0x100; ++i) {
            _bin2num[_toString(0xf700 + i)] = i;
        }
    })();
    _ie && document.write('<script type="text/vbscript">\
Function vblen(b)vblen=LenB(b.responseBody)End Function\n\
Function vbstr(b)vbstr=CStr(b.responseBody)+chr(0)End Function</' + 'script>');
})(this);
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
})(Stormancer || (Stormancer = {}));
(function ($, window) {
    Stormancer.jQueryWrapper.initWrapper($);
    $.stormancer = function (configuration) {
        return new Stormancer.Client(configuration);
    };
}(jQuery, window));
var Stormancer;
(function (Stormancer) {
    var WebSocketConnection = (function () {
        function WebSocketConnection(id, socket) {
            this.metadata = {};
            this.serializer = new Stormancer.MsgPackSerializer();
            this.id = id;
            this._socket = socket;
            this.connectionDate = new Date();
            this.state = 2 /* Connected */;
        }
        WebSocketConnection.prototype.close = function () {
            this._socket.close();
        };
        WebSocketConnection.prototype.sendSystem = function (msgId, data) {
            var bytes = new Uint8Array(data.length + 1);
            bytes[0] = msgId;
            bytes.set(data, 1);
            this._socket.send(bytes.buffer);
        };
        WebSocketConnection.prototype.sendToScene = function (sceneIndex, route, data, priority, reliability) {
            var bytes = new Uint8Array(data.length + 3);
            bytes[0] = sceneIndex;
            var ushorts = new Uint16Array(1);
            ushorts[0] = route;
            bytes.set(new Uint8Array(ushorts.buffer), 1);
            bytes.set(data, 3);
            this._socket.send(bytes.buffer);
        };
        WebSocketConnection.prototype.setApplication = function (account, application) {
            this.account = account;
            this.application = application;
        };
        return WebSocketConnection;
    })();
    Stormancer.WebSocketConnection = WebSocketConnection;
})(Stormancer || (Stormancer = {}));
var Stormancer;
(function (Stormancer) {
    var WebSocketTransport = (function () {
        function WebSocketTransport() {
            this.name = "websocket";
            this.isRunning = false;
            this._connecting = false;
            this.packetReceived = [];
            this.connectionOpened = [];
            this.connectionClosed = [];
        }
        WebSocketTransport.prototype.start = function (type, handler, token) {
            this._type = name;
            this._connectionManager = handler;
            this.isRunning = true;
            token.onCancelled(this.stop);
            var deferred = $.Deferred();
            deferred.resolve();
            return deferred.promise();
        };
        WebSocketTransport.prototype.stop = function () {
            this.isRunning = false;
            if (this._socket) {
                this._socket.close();
                this._socket = null;
            }
        };
        WebSocketTransport.prototype.connect = function (endpoint) {
            var _this = this;
            if (!this._socket && !this._connecting) {
                this._connecting = true;
                var socket = new WebSocket("ws://" + endpoint + "/");
                socket.binaryType = "arraybuffer";
                socket.onmessage = function (args) { return _this.onMessage(args.data); };
                this._socket = socket;
                var result = $.Deferred();
                socket.onclose = function (args) { return _this.onClose(result, args); };
                socket.onopen = function () { return _this.onOpen(result); };
                return result.promise();
            }
            throw new Error("This transport is already connected.");
        };
        WebSocketTransport.prototype.createNewConnection = function (socket) {
            var cid = this._connectionManager.generateNewConnectionId();
            return new Stormancer.WebSocketConnection(cid, socket);
        };
        WebSocketTransport.prototype.onOpen = function (deferred) {
            this._connecting = false;
            var connection = this.createNewConnection(this._socket);
            this._connectionManager.newConnection(connection);
            this.connectionOpened.map(function (action) {
                action(connection);
            });
            this._connection = connection;
            deferred.resolve(connection);
        };
        WebSocketTransport.prototype.onMessage = function (buffer) {
            var data = new Uint8Array(buffer);
            if (this._connection) {
                var packet = new Stormancer.Packet(this._connection, data);
                if (data[0] === Stormancer.MessageIDTypes.ID_CONNECTION_RESULT) {
                    this.id = data.subarray(1, 9);
                }
                else {
                    this.packetReceived.map(function (action) {
                        action(packet);
                    });
                }
            }
        };
        WebSocketTransport.prototype.onClose = function (deferred, closeEvent) {
            var _this = this;
            if (!this._connection) {
                this._connecting = false;
                deferred.reject(new Error("Can't connect WebSocket to server. Error code: " + closeEvent.code + ". Reason: " + closeEvent.reason + "."));
                this._socket = null;
            }
            else {
                var reason = closeEvent.wasClean ? "CLIENT_DISCONNECTED" : "CONNECTION_LOST";
                if (this._connection) {
                    this._connectionManager.closeConnection(this._connection, reason);
                    this.connectionClosed.map(function (action) {
                        action(_this._connection);
                    });
                }
            }
        };
        return WebSocketTransport;
    })();
    Stormancer.WebSocketTransport = WebSocketTransport;
})(Stormancer || (Stormancer = {}));
//# sourceMappingURL=stormancer.js.map