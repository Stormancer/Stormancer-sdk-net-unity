using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public class PeerDisconnectedException : Exception
    {
        public PeerDisconnectedException()
        {
        }

        public PeerDisconnectedException(string message) : base(message)
        {
        }

        public PeerDisconnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
