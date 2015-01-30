using Stormancer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Core;

namespace Stormancer.Networking
{
    /// <summary>
    /// Interface describing a message dispatcher.
    /// </summary>
    public interface IPacketDispatcher
    {

        void DispatchPacket(Packet packet);

        void AddPRocessor(IPacketProcessor processor);
    }
}
