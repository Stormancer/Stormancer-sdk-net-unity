using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Dto
{
    public struct ConnectedToSceneMsg
    {
        public byte Handle;
    }

    public struct ConnectToSceneMsg
    {
        public string Token;
        public List<RouteDto> Routes;
    }
}
