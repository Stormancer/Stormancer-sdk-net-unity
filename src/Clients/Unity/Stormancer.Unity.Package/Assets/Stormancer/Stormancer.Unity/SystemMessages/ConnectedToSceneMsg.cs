using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
#if UNITY_IOS
    public class ConnectToSceneMsg
#else
    public struct ConnectToSceneMsg
#endif
    {
        public string Token;
        public List<RouteDto> Routes;

        //public Dictionary<string, string> SceneMetadata { get; set; }

        public Dictionary<string, string> ConnectionMetadata { get; set; }

    }
}
