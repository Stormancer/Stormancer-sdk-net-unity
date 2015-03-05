module Cancellation {
    export class tokenSource {
        constructor() {
        }

        private data: sourceData = {
            reason: <string>null,
            isCancelled: false,
            listeners: []
        };

        public cancel(reason?: string): void {
            this.data.isCancelled = true;
            reason = reason || 'Operation Cancelled';

            this.data.reason = reason;

            setTimeout(function () {
                for (var i = 0; i < this.data.listeners.length; i++) {
                    if (typeof this.data.listeners[i] === 'function') {
                        this.data.listeners[i](reason);
                    }
                }
            }, 0);
        }

        public token: token = new token(this.data);
    }

    export class token {
        constructor(data: sourceData) {
            this.data = data;
        }

        private data: sourceData;

        public isCancelled(): boolean {
            return this.data.isCancelled;
        }

        public throwIfCancelled(): void {
            if (this.isCancelled()) {
                throw this.data.reason;
            }
        }

        public onCancelled(callBack: (reason: string) => void) {
            if (this.isCancelled()) {
                setTimeout(function () {
                    callBack(this.data.reason);
                }, 0);
            } else {
                this.data.listeners.push(callBack);
            }
        }
    }

    export interface sourceData {
        reason: string;
        isCancelled: boolean;
        listeners: ((reason: string) => void)[]
    }
}