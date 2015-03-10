/// <reference path="Stormancer.ts" />

module Stormancer {
    export interface IRequest {
        lastRefresh: Date;
        id: number;
        observer: IObserver<Packet<IConnection>>;
        deferred: JQueryDeferred<void>;
    }
}
