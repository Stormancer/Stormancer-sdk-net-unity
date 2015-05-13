using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// Class containing scene shutdown arguments
    /// </summary>
    public class ShutdownArgs
    {
        /// <summary>
        /// Creates a new ShutdownArgs instance
        /// </summary>
        /// <param name="reason">A string containing the shutdown reason</param>
        /// <param name="data">Custom data</param>
        /// <remarks>
        /// The shutdown custom data can be provided in the x-userdata header when calling the "delete scene" HTTP web API.
        /// </remarks>
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
