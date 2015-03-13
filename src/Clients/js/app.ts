class Greeter {
    element: HTMLElement;
    span: HTMLElement;
    timerToken: number;
    scene;

    constructor(element: HTMLElement) {
        this.element = element;
        this.element.innerHTML += "The time is: ";
        this.span = document.createElement('span');
        this.element.appendChild(this.span);
    }

    start() {
        var config = Stormancer.Configuration.forAccount("e376222c-f57c-6cae-8a4d-98fcca54122e", "test");
        config.serverEndpoint = "http://localhost:8081";        
        var client = $.stormancer(config);

        var scenePromise = client.getPublicScene("scene1", "antlafarge");

        var deferred = $.Deferred<string>();
        scenePromise.then(scene => {
            this.scene = scene;
            scene.addRoute("echo.out", this.messageReceived);

            return scene.connect().then(() => {
                this.timerToken = setInterval(() => {
                    var localDateString = new Date().toLocaleString();
                    this.span.innerHTML = localDateString;
                    this.sendMessage("echo.in", localDateString);
                }, 500);
            });
        });
    }

    sendMessage(routeName, message) {
        this.scene.sendPacket(routeName, new Uint8Array(message.split('').map(function (v) { return v.charCodeAt(0) })));
        console.log("Message sent on " + routeName + ":" + message);
    }

    messageReceived(packet) {
        console.log("Packet received :", packet);
        var message = packet.data.map(String.fromCharCode).join('');
        this.span.innerHTML = message;
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
