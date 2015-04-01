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
        /// <summary>
        /// Connects the user to a scene
        /// </summary>
        ID_CONNECT_TO_SCENE = DefaultMessageIDTypes.ID_USER_PACKET_ENUM,
        /// <summary>
        /// Used to disconnect the user from a scene
        /// </summary>
        ID_DISCONNECT_FROM_SCENE = 135,

        /// <summary>
        /// Retrives runtime informations about a scene
        /// </summary>
        ID_GET_SCENE_INFOS=136,

        /// <summary>
        /// Sends a reponse to a system request
        /// </summary>
        ID_REQUEST_RESPONSE_MSG=137 ,

        /// <summary>
        /// Sends a "request complete" message to close a system request channel
        /// </summary>
        ID_REQUEST_RESPONSE_COMPLETE=138,

        /// <summary>
        /// Sends an error as aresponse to a system request and close the request channel
        /// </summary>
        ID_REQUEST_RESPONSE_ERROR=139,

        /// <summary>
        /// Identifies a response to a connect to scene message
        /// </summary>
        ID_CONNECTION_RESULT=140,

        /// <summary>
        /// First id for scene handles
        /// </summary>
        ID_SCENES=141,

    }
}
