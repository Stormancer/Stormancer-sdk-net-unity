using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// Argument for the OnDisconnected event
    /// </summary>
    public class DisconnectedArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="client"></param>
        public DisconnectedArgs(string reason, IScenePeerClient client)
        {
            this.Peer = client;
            this.Reason = reason;
        }

        /// <summary>
        /// A string containing the disconnection reason.
        /// </summary>
        public string Reason { get; private set; }

        /// <summary>
        /// A `IScenePeerClient` object representing the disconnected peer.
        /// </summary>
        public IScenePeerClient Peer { get; private set; }
    }
}
