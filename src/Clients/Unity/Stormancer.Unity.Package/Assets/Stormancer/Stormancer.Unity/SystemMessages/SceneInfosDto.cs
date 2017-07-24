using System.Collections.Generic;

namespace Stormancer.Dto
{
#if UNITY_IOS
    public class SceneInfosRequestDto

#else
    public struct SceneInfosRequestDto
#endif
    {
        public string Token;
        public Dictionary<string, string> Metadata;
    }

#if UNITY_IOS
    public class SceneInfosDto
#else
    public struct SceneInfosDto
#endif
    {
        public string SceneId;

        public Dictionary<string, string> Metadata;

        public List<RouteDto> Routes;

        public string SelectedSerializer;
    }
}
