module Stormancer {
    export class SceneDispatcher implements IPacketProcessor {
        private _scenes: Scene[] = [];

        public registerProcessor(config: PacketProcessorConfig): void {
            config.addCatchAllProcessor(this.handler);
        }

        private handler(sceneHandler: number, packet: Packet<IConnection>): boolean {
            if (sceneHandler < MessageIDTypes.ID_SCENES) {
                return false;
            }
            var scene = this._scenes[sceneHandler - MessageIDTypes.ID_SCENES];
            if (!scene) {
                return false;
            } else {
                packet.setMetadataValue("scene", scene);
                scene.handleMessage(packet);
                return true;
            }
        }

        public addScene(scene: Scene): void {
            this._scenes[scene.handle - MessageIDTypes.ID_SCENES] = scene;
        }

        public removeScene(sceneHandle: number) {
            delete this._scenes[sceneHandle - MessageIDTypes.ID_SCENES];
        }
    }
}
