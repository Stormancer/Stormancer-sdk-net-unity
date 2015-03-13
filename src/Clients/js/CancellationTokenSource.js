var Cancellation;
(function (Cancellation) {
    var tokenSource = (function () {
        function tokenSource() {
            this.data = {
                reason: null,
                isCancelled: false,
                listeners: []
            };
            this.token = new token(this.data);
        }
        tokenSource.prototype.cancel = function (reason) {
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
        };
        return tokenSource;
    })();
    Cancellation.tokenSource = tokenSource;
    var token = (function () {
        function token(data) {
            this.data = data;
        }
        token.prototype.isCancelled = function () {
            return this.data.isCancelled;
        };
        token.prototype.throwIfCancelled = function () {
            if (this.isCancelled()) {
                throw this.data.reason;
            }
        };
        token.prototype.onCancelled = function (callBack) {
            if (this.isCancelled()) {
                setTimeout(function () {
                    callBack(this.data.reason);
                }, 0);
            }
            else {
                this.data.listeners.push(callBack);
            }
        };
        return token;
    })();
    Cancellation.token = token;
})(Cancellation || (Cancellation = {}));
//# sourceMappingURL=CancellationTokenSource.js.map