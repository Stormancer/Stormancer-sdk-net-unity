class Greeter {
    element: HTMLElement;
    span: HTMLElement;
    timerToken: number;
    scene: any;

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
            scene.addRoute("echo.out", packet => {
                console.log(packet.data[0]);
                console.log(packet.data.length);
            });

            return scene.connect().then(() => {
                this.timerToken = setInterval(() => {
                    var localDateString = new Date().toLocaleString();
                    this.span.innerHTML = localDateString;
                    this.send(localDateString);
                }, 500);
            });            
        });
    }

    send(message) {
        this.scene.sendPacket("echo.in", new Uint8Array(message.split('')));
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
