module Stormancer {
    export interface IRequestModule {
        register(builder: (msgId: number, handler: (context: RequestContext) => JQueryPromise<void>) => void): void;
    }
}
