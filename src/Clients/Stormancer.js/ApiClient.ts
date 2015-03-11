module Stormancer {
    export class ApiClient {
        constructor(config: Configuration, tokenHandler: ITokenHandler) {
            this._config = config;
            this._tokenHandler = tokenHandler;
        }

        private _config: Configuration;
        private createTokenUri = "/{0}/{1}/scenes/{2}/token";
        private _tokenHandler: ITokenHandler;

        public getSceneEndpoint<T>(accountId: string, applicationName: string, sceneId: string, userData: T): JQueryPromise<SceneEndpoint> {
            var serializer = new MsgPackSerializer();
            var data: Uint8Array = serializer.serialize(userData);

            var url = this._config.getApiEndpoint() + Helpers.stringFormat(this.createTokenUri, accountId, applicationName, sceneId);
            return $.ajax({
                type: "POST",
                url: url,
                contentType: "application/msgpack",
                headers: {
                    "Accept": "application/json",
                    "x-version": "1.0.0"
                },
                data: data
            }).then(result => {
                return this._tokenHandler.decodeToken(result);
            });
        }
    }
}
