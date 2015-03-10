/// <reference path="Stormancer.ts" />

module Stormancer {
    class Packet<T> {
        constructor(source: T, data: Uint8Array, metadata?: IMap<any>) {
            this.connection = source;
            this.data = data;
            this._metadata = metadata;
        }

        // Data contained in the packet.
        public data: Uint8Array;

        private _metadata: IMap<any>;

        public setMetadata(metadata: IMap<any>) {
            this._metadata = metadata;
        }

        public getMetadata(): IMap<any> {
            if (!this._metadata) {
                this._metadata = {};
            }
            return this._metadata;
        }

        public setMetadataValue(key: string, value): void {
            if (!this._metadata) {
                this._metadata = {};
            }
            this._metadata[key] = value;
        }

        public getMetadataValue(key: string): any {
            if (!this._metadata) {
                this._metadata = {};
            }
            return this._metadata[key];
        }

        public connection: T;
    }
}
