using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Client45
{
    enum SystemRequestIDTypes : byte
    {
        ID_GET_SCENE_INFOS = 136,
        ID_CONNECT_TO_SCENE = 134,
        ID_SET_METADATA = 0,
        ID_SCENE_READY = 1,
        ID_PING = 2,
        ID_DISCONNECT_FROM_SCENE = 135,
       
    }
}
