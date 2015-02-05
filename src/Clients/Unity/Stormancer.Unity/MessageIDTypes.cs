using RakNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Networking
{
    /// <metadata visibility="internal"/>
    /// <summary>
    /// Message types understood by the agent.
    /// </summary>
    public enum MessageIDTypes : byte
    {
        ID_CONNECT_TO_SCENE = DefaultMessageIDTypes.ID_USER_PACKET_ENUM,
        ID_DISCONNECT_FROM_SCENE,
        ID_GET_SCENE_INFOS,
        ID_REQUEST_RESPONSE_MSG ,
        ID_REQUEST_RESPONSE_COMPLETE,
        ID_REQUEST_RESPONSE_ERROR,
        ID_CONNECTION_RESULT,


        ID_SCENES,


    }
}
