/// <reference path="../Helpers.ts" />

module Stormancer {
    export interface Request {
        lastRefresh: Date;
        id: number;
        observer: IObserver<Packet<IConnection>>;
        deferred: JQueryDeferred<void>;
    }
}
