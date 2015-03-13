module Stormancer {
    export class WebSocketTransport implements ITransport {
        public name: string = "websocket";
        public id: Uint8Array;
        // Gets a boolean indicating if the transport is currently running.
        public isRunning: boolean = false;

        private _type: string;
        private _connectionManager: IConnectionManager;
        private _socket: WebSocket;
        private _connecting = false;
        private _connection: WebSocketConnection;

        // Fires when the transport recieves new packets.
        public packetReceived: ((packet: Packet<IConnection>) => void)[] = [];

        // Fires when a remote peer has opened a connection.
        public connectionOpened: ((connection: IConnection) => void)[] = [];

        // Fires when a connection to a remote peer is closed.
        public connectionClosed: ((connection: IConnection) => void)[] = [];
        
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

                var socket = new WebSocket("ws://" + endpoint + "/");
                socket.binaryType = "arraybuffer";

                socket.onmessage = args => this.onMessage(args.data);

                this._socket = socket;

                var result = $.Deferred<IConnection>();

                socket.onclose = args => this.onClose(result, args);
                socket.onopen = () => this.onOpen(result);
                return result.promise();
            }
            throw new Error("This transport is already connected.");
        }



        private createNewConnection(socket: WebSocket): WebSocketConnection {
            var cid = this._connectionManager.generateNewConnectionId();
            return new WebSocketConnection(cid, socket);
        }

        private onOpen(deferred: JQueryDeferred<IConnection>) {
            this._connecting = false;

            var connection = this.createNewConnection(this._socket);

            this._connectionManager.newConnection(connection);

            this.connectionOpened.map(action => {
                action(connection);
            });

            this._connection = connection;

            deferred.resolve(connection);
        }

        private onMessage(buffer: ArrayBuffer) {
            var data = new Uint8Array(buffer);
            if (this._connection) {
                var packet = new Packet<IConnection>(this._connection, data);

                if (data[0] === MessageIDTypes.ID_CONNECTION_RESULT) {
                    this.id = data.subarray(1, 9);
                }
                else {
                    this.packetReceived.map(action => {
                        action(packet);
                    });
                }
            }
        }

        private onClose(deferred: JQueryDeferred<IConnection>, closeEvent: CloseEvent) {
            if (!this._connection) {
                this._connecting = false;

                deferred.reject(new Error("Can't connect WebSocket to server. Error code: " + closeEvent.code + ". Reason: " + closeEvent.reason + "."));
                this._socket = null;
            }
            else {
                var reason = closeEvent.wasClean ? "CLIENT_DISCONNECTED" : "CONNECTION_LOST";

                if (this._connection) {
                    this._connectionManager.closeConnection(this._connection, reason);

                    this.connectionClosed.map(action => {
                        action(this._connection);
                    });
                }
            }
        }
    }
}
