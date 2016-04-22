using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer.Core;

namespace Stormancer.Processors
{
    class RouteScenePacketHandler : IScenePacketHandler
    {
        public bool HandlePacket(Packet packet, Scene scene)
        {
            return scene.HandleMessage(packet);
        }
    }
}
