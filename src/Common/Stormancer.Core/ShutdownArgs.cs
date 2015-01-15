using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Core
{
    /// <summary>
    /// Class containing scene shutdown arguments
    /// </summary>
    public class ShutdownArgs
    {
        public ShutdownArgs(string reason, string data)
        {
            Reason = reason;
            Data = data;
        }

        /// <summary>
        /// Reason why the scene is shutting down.
        /// </summary>
        public string Reason { get; private set; }

        /// <summary>
        /// Custom data
        /// </summary>
        /// <remarks>
        /// In case of shutdown following scene deletion, custom data can be provided as part of the deletion request in a 'x-userdata' beader.
        /// </remarks>
        public string Data { get; private set; }

       
    }
}
