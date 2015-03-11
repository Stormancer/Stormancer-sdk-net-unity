module Stormancer {
    export interface SceneInfosRequestDto {
        Token: string;
        Metadata: Map;
    }

    export interface SceneInfosDto {
        SceneId: string;
        Metadata: Map;
        Routes: RouteDto[];
        SelectedSerializer: string;
    }
}
