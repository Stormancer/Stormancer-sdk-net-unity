module Stormancer {
    export class Route {
        public handlers: ((packet: Packet<IConnection>) => void)[] = [];

        public constructor(public scene: IScene, public name: string, public index = 0, public metadata: Map = {}) {
        }
    }
}
