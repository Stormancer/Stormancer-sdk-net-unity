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
    public static class PacketExtensions
    {
        /// <summary>
        /// Attempts to deserialize data in the packet
        /// </summary>
        /// <typeparam name="T">The expected type for the data in the packet.</typeparam>
        /// <param name="packet">The target packet.</param>
        /// <returns>A `T` object.</returns>
        /// <remarks>The method reads the packet stream during the deserialization process.</remarks>
        public static T ToObject<T>(this Packet packet)
        {
            return packet.Serializer().Deserialize<T>(packet.Stream);
        }
        public static T ReadObject<T>(this Packet<IScenePeer> packet)
        {
            return packet.Serializer().Deserialize<T>(packet.Stream);
        }

      
        public static ISerializer Serializer(this Packet packet)
        {
            return packet.Connection.Serializer();
        }
        public static ISerializer Serializer(this Packet<IScenePeer> packet)
        {
            return packet.Connection.Serializer();
        }
        public static ISerializer Serializer(this IScenePeer c)
        {
            return c.GetComponent<ISerializer>();
        }
        public static ISerializer Serializer(this IConnection c)
        {
            return c.GetComponent<ISerializer>();
        }
    }
}
