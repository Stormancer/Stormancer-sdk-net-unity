using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.p2p
{
    interface IP2pMediator
    {
      

        Task<IDisposable> RegisterP2PServer(string p2pServerId, string port, NetworkProtocol protocol, string host = "*");

        Task<IP2PServerEndpoint> OpenP2PConnection(string token, string p2pServerId);

    }
}
