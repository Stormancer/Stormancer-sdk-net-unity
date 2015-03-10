/// <reference path="stormancer.ts" />
var Greeter = (function () {
    function Greeter(element) {
        this.element = element;
        this.element.innerHTML += "The time is: ";
        this.span = document.createElement('span');
        this.element.appendChild(this.span);
        this.span.innerText = new Date().toUTCString();
    }
    Greeter.prototype.start = function () {
        var _this = this;
        this.timerToken = setInterval(function () { return _this.span.innerHTML = new Date().toUTCString(); }, 500);
        var ConsoleColor = {};
        this.Write("Creating client");
        var config = Stormancer.Configuration.forAccount("e376222c-f57c-6cae-8a4d-98fcca54122e", "test");
        config.serverEndpoint = "http://localhost:8081";
        var client = $.stormancer(config);
        this.Write("Done", ConsoleColor.Green);
        this.Write("Get scene", ConsoleColor.White);
        var scenePromise = client.getPublicScene("scene1", "antlafarge");
        this.Write("Done", ConsoleColor.Green);
        this.Write("add 'echo.out' route", ConsoleColor.White);
        var deferred = $.Deferred();
        scenePromise.then(function (scene) {
            //var tcs = new TaskCompletionSource<string>();
            scene.addRoute("echo.out", function (packet) {
                _this.Write(packet.data[0]);
                _this.Write(packet.data.length);
            });
            _this.Write("Done", ConsoleColor.Green);
            _this.Write("Connect", ConsoleColor.White);
            return scene.connect().then(function () {
                scene.sendPacket("echo.in", new Uint8Array([1]));
            });
        });
    };
    Greeter.prototype.Write = function (message) {
        var color = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            color[_i - 1] = arguments[_i];
        }
        var child = document.createElement("div");
        child.innerText = message;
        document.querySelector("#log").appendChild(child);
    };
    Greeter.prototype.stop = function () {
        clearTimeout(this.timerToken);
    };
    return Greeter;
})();
window.onload = function () {
    var el = document.getElementById('content');
    var greeter = new Greeter(el);
    greeter.start();
};
//# sourceMappingURL=app.js.map