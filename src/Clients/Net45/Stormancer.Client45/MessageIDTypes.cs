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
    internal enum MessageIDTypes : byte
    {
        /// <summary>
        /// System request
        /// </summary>
        ID_SYSTEM_REQUEST = DefaultMessageIDTypes.ID_USER_PACKET_ENUM,

        /// <summary>
        /// reponse to a system request
        /// </summary>
        ID_REQUEST_RESPONSE_MSG =137 ,

        /// <summary>
        /// "request complete" message to close a system request channel
        /// </summary>
        ID_REQUEST_RESPONSE_COMPLETE=138,

        /// <summary>
        ///  error as a response to a system request and close the request channel
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
