/// <reference path="Stormancer.ts" />

module Stormancer {
    // Contains method to register handlers for message types when passed to the IPacketProcessor.RegisterProcessor method.
    export class PacketProcessorConfig {
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
            this._defaultProcessors.push((n, p) => handler(n, p));
        }
    }

    export interface IPacketProcessor {
        registerProcessor(config: PacketProcessorConfig): void;
    }
}
