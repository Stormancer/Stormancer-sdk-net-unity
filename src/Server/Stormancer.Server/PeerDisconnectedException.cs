using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Triggered when the peer is disconnected
    /// </summary>
    public class PeerDisconnectedException : Exception
    {
        /// <summary>
        /// PeerDisconnectedException constructor
        /// </summary>
        public PeerDisconnectedException()
        {
        }

        /// <summary>
        /// PeerDisconnectedException constructor
        /// </summary>
        /// <param name="message"></param>
        public PeerDisconnectedException(string message) : base(message)
        {
        }

        /// <summary>
        /// PeerDisconnectedException constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public PeerDisconnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
