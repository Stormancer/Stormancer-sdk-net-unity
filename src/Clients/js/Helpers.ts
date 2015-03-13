module Stormancer {
    export interface Map {
        [key: string]: string;
    }

    export interface IMap<T> {
        [key: string]: T;
    }

    export class Helpers {
        static base64ToByteArray(data: string): Uint8Array {
            return new Uint8Array(atob(data).split('').map(function (c) { return c.charCodeAt(0) }));
        }

        static stringFormat(str: string, ...args: any[]): string {
            for (var i in args) {
                str = str.replace('{' + i + '}', args[i]);
            }
            return str;
        }

        static mapKeys(map: { [key: string]: any }): string[] {
            var keys: string[] = [];
            for (var key in map) {
                if (map.hasOwnProperty(key)) {
                    keys.push(key);
                }
            }
            return keys;
        }

        static mapValues<T>(map: IMap<T>): T[] {
            var result: T[] = [];
            for (var key in map) {
                result.push(map[key]);
            }

            return result;
        }

        static promiseFromResult<T>(result: T): JQueryPromise<T> {
            var deferred = jQuery.Deferred();
            deferred.resolve(result);
            return deferred.promise();
        }

        static promiseIf(condition: boolean, action: () => JQueryPromise<void>, context?: any): JQueryPromise<void> {
            if (condition) {
                if (context) {
                    return action.call(context);
                }
                else {
                    return action();
                }
            } else {
                return Helpers.promiseFromResult(null);
            }
        }
    }

    export interface IObserver<T> {
        onCompleted(): void;
        onError(error: any): void;
        onNext(value: T): void;
    }
}
