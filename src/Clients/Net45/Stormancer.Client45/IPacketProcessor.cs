using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !StormancerClient
using Stormancer.Platform.Core.Composition;
#endif
namespace Stormancer.Networking
{
    /// <summary>
    /// Represents a packet processor
    /// </summary>
    /// <remarks>
    /// Packet processors handle packets received from remote peers
    /// </remarks>
#if !StormancerClient
    [Dependency]
#endif
    public interface IPacketProcessor
    {
        /// <summary>
        /// Method called by the packet dispatcher to register the packet processor
        /// </summary>
        /// <param name="config"></param>
        void RegisterProcessor(PacketProcessorConfig config);
    }


   /// <summary>
   /// Contains method to register handlers for message types when passed to the IPacketProcessor.RegisterProcessor method.
   /// </summary>
    public class PacketProcessorConfig
    {
        private readonly IDictionary<byte, Func<Packet, bool>> _handlers;
        private readonly List<Func<byte, Packet, bool>> _defaultProcessors;
        internal PacketProcessorConfig(IDictionary<byte, Func<Packet, bool>> handlers, List<Func<byte, Packet, bool>> defaultprocessors)
        {
            _handlers = handlers;
            _defaultProcessors = defaultprocessors;
        }

        /// <summary>
        /// Adds an handler for the specified message type.
        /// </summary>
        /// <param name="msgId">A byte representing the message type.</param>
        /// <param name="handler">A Func&lt;Packet,bool&gt; instance to be executed when receiving a packet with the message type.</param>
        public void AddProcessor(byte msgId, Func<Packet, bool> handler)
        {
            if (_handlers.ContainsKey(msgId))
            {
                throw new ArgumentException(string.Format("An handler is already registered for id {0}", msgId));
            }
            _handlers.Add(msgId, handler);
        }

        /// <summary>
        /// Adds
        /// </summary>
        /// <param name="handler"></param>
        public void AddCatchAllProcessor(Func<byte, Packet, bool> handler)
        {
            _defaultProcessors.Add(handler);
        }
    }
}
