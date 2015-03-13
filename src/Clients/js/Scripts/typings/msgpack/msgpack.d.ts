// Interface
declare class msgpack {
    static pack(data: any, settings?: MsgPackSettings): number[];
    static unpack(data: Uint8Array, settings?: MsgPackSettings): any;
}

interface MsgPackSettings {
    byteProperties: string[];
}
