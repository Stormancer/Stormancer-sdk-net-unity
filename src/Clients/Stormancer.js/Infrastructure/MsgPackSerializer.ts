/// <reference path="Stormancer.ts" />

module Stormancer {
    export class MsgPackSerializer implements ISerializer {
        public serialize<T>(data: T): Uint8Array {
            return new Uint8Array(msgpack.pack(data));
        }

        public deserialize<T>(bytes: Uint8Array): T {
            return msgpack.unpack(bytes);
        }

        name: string = "msgpack/map";
    }
}
