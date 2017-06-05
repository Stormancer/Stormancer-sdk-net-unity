using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.p2p
{
    class P2pConnection
    {
        public P2pConnection(string uid, string serverId)
        {

            ServerId = serverId;
        }

        public string UId { get; }
        public string ServerId { get; }
    }
}
