/// <reference path="Stormancer.ts" />

module Stormancer {
    class DefaultPacketDispatcher implements IPacketDispatcher {
        private _handlers: IMap<(packet: Packet<IConnection>) => boolean> = {};
        private _defaultProcessors: ((msgType: number, packet: Packet<IConnection>) => boolean)[] = [];

        public dispatchPacket(packet: Packet<IConnection>): void {
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
        }

        public addProcessor(processor: IPacketProcessor): void {
            processor.registerProcessor(new PacketProcessorConfig(this._handlers, this._defaultProcessors));
        }
    }
}
