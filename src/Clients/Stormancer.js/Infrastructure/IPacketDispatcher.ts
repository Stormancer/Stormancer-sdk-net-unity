module Stormancer {
    export interface IPacketDispatcher {
        dispatchPacket(packet: Packet<IConnection>): void;

        addProcessor(processor: IPacketProcessor): void;
    }
}
