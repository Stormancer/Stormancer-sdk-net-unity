/// <reference path="stormancer.ts" />

class Greeter {
    element: HTMLElement;
    span: HTMLElement;
    timerToken: number;

    constructor(element: HTMLElement) {
        this.element = element;
        this.element.innerHTML += "The time is: ";
        this.span = document.createElement('span');
        this.element.appendChild(this.span);
        this.span.innerText = new Date().toUTCString();
    }

    start() {
        this.timerToken = setInterval(() => this.span.innerHTML = new Date().toUTCString(), 500);
        var ConsoleColor: any = {};
        this.Write("Creating client");
        var config = Stormancer.Configuration.forAccount("e376222c-f57c-6cae-8a4d-98fcca54122e", "test");
        config.serverEndpoint = "http://localhost:8081";        
        var client = $.stormancer(config);
        this.Write("Done", ConsoleColor.Green);

        this.Write("Get scene", ConsoleColor.White);
        var scenePromise = client.getPublicScene("scene1", "antlafarge");
        this.Write("Done", ConsoleColor.Green);

        this.Write("add 'echo.out' route", ConsoleColor.White);
        var deferred = $.Deferred<string>();

        scenePromise.then(scene => {
            //var tcs = new TaskCompletionSource<string>();
            scene.addRoute("echo.out", packet => {
                this.Write(packet.data[0]);
                this.Write(packet.data.length);
            });

           
            this.Write("Done", ConsoleColor.Green);

            this.Write("Connect", ConsoleColor.White);
            return scene.connect().then(() => {
                scene.sendPacket("echo.in", new Uint8Array([1]));
            });            
        });
    }

    Write(message, ...color) {
        var child = document.createElement("div");
        child.innerText = message;
        document.querySelector("#log").appendChild(child);
    }

    stop() {
        clearTimeout(this.timerToken);
    }
}

window.onload = () => {
    var el = document.getElementById('content');
    var greeter = new Greeter(el);
    greeter.start();
};
