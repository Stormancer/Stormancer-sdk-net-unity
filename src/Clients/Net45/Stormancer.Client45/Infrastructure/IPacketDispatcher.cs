using Stormancer.Networking;
#if !StormancerClient
using Stormancer.Platform.Core.Composition;
#endif
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
#if !StormancerClient
    [Dependency]
#endif
    public interface IPacketDispatcher
    {
        /// <summary>
        /// Dispatches a packet to the system
        /// </summary>
        /// <param name="packet"></param>
        void DispatchPacket(Packet packet);

        /// <summary>
        /// Adds a packet processor to the dispatcher
        /// </summary>
        /// <param name="processor">An `IPacketProcessor` object</param>
        void AddPRocessor(IPacketProcessor processor);
    }
}
