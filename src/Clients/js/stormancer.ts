/// <reference path="Scripts/msgPack.ts" />

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
