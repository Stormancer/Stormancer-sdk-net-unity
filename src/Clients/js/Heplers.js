/// <reference path="Stormancer.ts" />
var Stormancer;
(function (Stormancer) {
    var Helpers = (function () {
        function Helpers() {
        }
        Helpers.base64ToByteArray = function (data) {
            return new Uint8Array(atob(data).split('').map(function (c) {
                return c.charCodeAt(0);
            }));
        };
        Helpers.stringFormat = function (str) {
            var args = [];
            for (var _i = 1; _i < arguments.length; _i++) {
                args[_i - 1] = arguments[_i];
            }
            for (var i in args) {
                str = str.replace('{' + i + '}', args[i]);
            }
            return str;
        };
        Helpers.mapKeys = function (map) {
            var keys = [];
            for (var key in map) {
                if (map.hasOwnProperty(key)) {
                    keys.push(key);
                }
            }
            return keys;
        };
        Helpers.mapValues = function (map) {
            var result = [];
            for (var key in map) {
                result.push(map[key]);
            }
            return result;
        };
        Helpers.promiseFromResult = function (result) {
            var deferred = jQuery.Deferred();
            deferred.resolve(result);
            return deferred.promise();
        };
        Helpers.promiseIf = function (condition, action, context) {
            if (condition) {
                if (context) {
                    return action.call(context);
                }
                else {
                    return action();
                }
            }
            else {
                return Helpers.promiseFromResult(null);
            }
        };
        return Helpers;
    })();
    Stormancer.Helpers = Helpers;
})(Stormancer || (Stormancer = {}));
//# sourceMappingURL=Heplers.js.map