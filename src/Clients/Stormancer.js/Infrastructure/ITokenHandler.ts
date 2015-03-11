module Stormancer {
    export interface ITokenHandler {
        decodeToken(token: string): SceneEndpoint;
    }

    export class TokenHandler implements ITokenHandler {
        private _tokenSerializer: ISerializer;

        public constructor() {
            this._tokenSerializer = new MsgPackSerializer();
        }

        public decodeToken(token: string): SceneEndpoint {
            var data = token.split('-')[0];
            var buffer = Helpers.base64ToByteArray(data);
            var result = this._tokenSerializer.deserialize<any[]>(buffer);

            var sceneEndpoint = new SceneEndpoint();
            sceneEndpoint.token = token;
            sceneEndpoint.tokenData = result;
            return sceneEndpoint;
        }
    }
}
