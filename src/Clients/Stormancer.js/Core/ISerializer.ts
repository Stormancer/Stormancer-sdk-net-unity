/// <reference path="Stormancer.ts" />

module Stormancer {
    // Contract for the binary serializers used by Stormancer applications.
    interface ISerializer {
        // Serialize an object into a stream.
        serialize<T>(data: T): Uint8Array;

        // Deserialize an object from a stream.
        deserialize<T>(bytes: Uint8Array): T;

        // The serializer format.
        name: string;
    }
}
