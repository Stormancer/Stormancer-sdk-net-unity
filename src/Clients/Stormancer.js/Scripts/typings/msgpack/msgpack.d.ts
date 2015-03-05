// Interface
declare class msgpack {
    static pack(data: any,settings: MsgPackSettings): Array<any>;
    static unpack(data: Array<any>,settings: MsgPackSettings): any;
}

interface MsgPackSettings {

    byteProperties: string[];
}