using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Contains logic that
    /// </summary>
    public interface IScenePacketHandler
    {
        /// <summary>
        /// Method containing the packet handling logic. 
        /// </summary>
        /// <param name="packet">The packet to handle</param>
        /// <param name="scene">The scene instance that was matched by the routing system for the packet.</param>
        /// <returns>Return true if the packet needs further handling, false if not.</returns>
        bool HandlePacket(Packet packet, Scene scene);
    }

    public enum HandlingResult
    {
        /// <summary>
        /// The handler wasn't abe
        /// </summary>
        NotHandled,
        Modified,
        Handled,
    }
}
