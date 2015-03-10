using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
    public struct SceneInfosRequestDto
    {
        public string Token;
        public Dictionary<string, string> Metadata;
    }

    public struct SceneInfosDto
    {
        public string SceneId;

        public Dictionary<string, string> Metadata;

        public List<RouteDto> Routes;

        public string SelectedSerializer;
    }
}
