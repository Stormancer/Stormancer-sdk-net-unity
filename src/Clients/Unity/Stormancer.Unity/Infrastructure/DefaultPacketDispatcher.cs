using Stormancer.Core;
using Stormancer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Networking
{
    public class DefaultPacketDispatcher: IPacketDispatcher
    {

        private Dictionary<byte, Func<Packet, bool>> _handlers = new Dictionary<byte, Func<Packet, bool>>();
        private List<Func<byte,Packet, bool>> _defaultProcessors = new List<Func<byte,Packet, bool>>();
        public void DispatchPacket(Packet packet)
        {
            Task.Factory.StartNew(() =>
            {
                bool processed = false;
                int count = 0;
                byte msgType = 0;
                while (!processed && count < 40)//Max 40 layers
                {

                    msgType = (byte)packet.Stream.ReadByte();
                    Func<Packet, bool> handler;
                    if (_handlers.TryGetValue(msgType, out handler))
                    {
                        processed = _handlers[msgType](packet);
                        count++;
                    }
                    else
                    {
                       break;
                    }
                }
                foreach (var processor in _defaultProcessors)
                {
                    if (processor(msgType,packet))
                    {
                        processed = true;
                        break;
                    }
                }
                if (processed == false)
                {
                    throw new NotSupportedException(string.Format("Couldn't process message. msgId: {0}", msgType));
                }
            });
        }

        public void AddProcessor(IPacketProcessor processor)
        {
            processor.RegisterProcessor(new PacketProcessorConfig(_handlers, _defaultProcessors));
        }

   
    }
}
