/// <reference path="Scripts/typings/msgpack/msgpack.d.ts" />
/// <reference path="CancellationTokenSource.ts" />
/// <reference path="ApiClient.ts" />
/// <reference path="Client.ts" />
/// <reference path="IConnectionManager.ts" />
/// <reference path="ILogger.ts" />
/// <reference path="IPacketProcessor.ts" />
/// <reference path="ITransport.ts" />
/// <reference path="MessageIDTypes.ts" />
/// <reference path="Scene.ts" />
/// <reference path="SceneEndpoint.ts" />
/// <reference path="ScenePeer.ts" />

// Module
module Stormancer {
    export class jQueryWrapper {
        static $: JQueryStatic;
        static initWrapper(jquery: JQueryStatic) {
            jQueryWrapper.$ = jquery;
        }
    }
}

interface JQueryStatic {
    stormancer: (configuration: Stormancer.Configuration) => Stormancer.IClient;
}

(function ($, window) {
    Stormancer.jQueryWrapper.initWrapper($);
    $.stormancer = (configuration: Stormancer.Configuration) => { return new Stormancer.Client(configuration); };
    //jQuery.support.cors = true
} (jQuery, window));
