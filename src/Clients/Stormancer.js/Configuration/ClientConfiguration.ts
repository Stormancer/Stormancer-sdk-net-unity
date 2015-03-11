module Stormancer {
    // Represents the configuration of a Stormancer client.
    export class Configuration {
        constructor() {
            //this.dispatcher = new PacketDispatcher();
            this.transport = new WebSocketTransport();
            this.dispatcher = new DefaultPacketDispatcher();
            this.serializers = [];
            this.serializers.push(new MsgPackSerializer());
        }

        static apiEndpoint: string = "http://api.stormancer.com/";

        
        
        // A string containing the target server endpoint.
        // This value overrides the *IsLocalDev* property.
        public serverEndpoint: string;

        // A string containing the account name of the application.
        public account: string;

        // A string containing the name of the application.
        public application: string;

        getApiEndpoint(): string {
            return this.serverEndpoint ? this.serverEndpoint : Configuration.apiEndpoint;

        }

        
        // Creates a ClientConfiguration object targeting the public online platform.
        static forAccount(accountId: string, applicationName: string): Configuration {
            var config = new Configuration();
            config.account = accountId;
            config.application = applicationName;
            return config;
        }

        public metadata: Map = {};

        // Adds metadata to the connection.
        public Metadata(key: string, value: string): Configuration {
            this.metadata[key] = value;
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
}
