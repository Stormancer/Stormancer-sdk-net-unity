/// <reference path="Stormancer.ts" />

module Stormancer {
    interface SceneInfosRequestDto {
        Token: string;
        Metadata: Map;
    }

    interface SceneInfosDto {
        SceneId: string;
        Metadata: Map;
        Routes: RouteDto[];
        SelectedSerializer: string;
    }
}
