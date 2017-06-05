using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.p2p
{
    interface IP2pServers
    {
        void RegisterLocalServer(string id, string host, ushort port, NetworkProtocol protocol);

        void RemoveLocalServer(string id);

        void AddClientConnection(string id, P2pConnection connection);
        void RemoveClientConnection(string id, P2pConnection connection);
    }
}
