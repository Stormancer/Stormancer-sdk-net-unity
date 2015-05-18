using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// Extensions for the packet class
    /// </summary>
    public static class PacketHostExtensions
    {
        /// <summary>
        /// Reads an object from the packet
        /// </summary>
        /// <typeparam name="T">The expected type for the data in the packet.</typeparam>
        /// <param name="packet">The target packet.</param>
        /// <returns>A `T` object.</returns>
   
        public static T ReadObject<T>(this Packet<IScenePeerClient> packet)
        {
            return packet.Serializer().Deserialize<T>(packet.Stream);
        }

        /// <summary>
        /// Gets the serializer to use with a packet
        /// </summary>
        /// <param name="packet">A packet object</param>
        /// <returns>A serializer compatible with the packet's content</returns>
        public static ISerializer Serializer(this Packet<IScenePeerClient> packet)
        {
            return packet.Connection.Serializer();
        }
    }
}
