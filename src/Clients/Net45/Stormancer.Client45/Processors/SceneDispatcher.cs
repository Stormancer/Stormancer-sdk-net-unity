using Stormancer.Core;
using Stormancer.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Processors
{
    internal class SceneDispatcher : IPacketProcessor
    {
        private Scene[] _scenes = new Scene[(int)byte.MaxValue - (int)MessageIDTypes.ID_SCENES + 1];
        private ConcurrentDictionary<byte, ConcurrentQueue<Packet>> _waitingPackets = new ConcurrentDictionary<byte, ConcurrentQueue<Packet>>();

        private IEnumerable<IScenePacketHandler> _packetHandlers;

        public SceneDispatcher(IEnumerable<IScenePacketHandler> handlers)
        {
            _packetHandlers = handlers;
        }
        public void RegisterProcessor(PacketProcessorConfig config)
        {
            config.AddCatchAllProcessor(Handler);
        }

        

        private bool Handler(byte sceneHandle, Packet packet)
        {
            if(sceneHandle < (byte)MessageIDTypes.ID_SCENES)
            {
                return false;
            }
            var scene = _scenes[sceneHandle - (byte)MessageIDTypes.ID_SCENES];
            if (scene == null)
            {
                var queue = _waitingPackets.GetOrAdd(sceneHandle, handle => new ConcurrentQueue<Packet>());
                queue.Enqueue(packet);
                return true;
            }
            else
            {
                HandlePacket(scene, packet);
                return true;
            }

        }

        public void AddScene(Scene scene)
        {
            _scenes[scene.Handle - (byte)MessageIDTypes.ID_SCENES] = scene;
            ConcurrentQueue<Packet> waitingPackets;
            if(_waitingPackets.TryRemove(scene.Handle, out waitingPackets))
            {
                Packet packet;
                while (waitingPackets.TryDequeue(out packet))
                {
                    HandlePacket(scene, packet);
                }
            }
         
        }

        public void RemoveScene(byte sceneHandle)
        {
            _scenes[sceneHandle - (byte)MessageIDTypes.ID_SCENES] = null;
        }

        private void HandlePacket(Scene scene, Packet packet)
        {
            packet.Metadata["scene"] = scene;

            foreach(var handler in _packetHandlers)
            {
                if(handler.HandlePacket(packet, scene))
                {
                    break;
                }
            }
            
        }
    }
}
