/// <reference path="Stormancer.ts" />

module Stormancer {
    interface IPacketDispatcher {
        dispatchPacket(packet: Packet<IConnection>): void;

        addProcessor(processor: IPacketProcessor): void;
    }
}
