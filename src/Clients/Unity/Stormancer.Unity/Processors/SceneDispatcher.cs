using Stormancer.Core;
using Stormancer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Processors
{
    internal class SceneDispatcher : IPacketProcessor
    {
        private Scene[] _scenes = new Scene[256];

        public void RegisterProcessor(PacketProcessorConfig config)
        {
            config.AddCatchAllProcessor(Handler);
        }

        private bool Handler(byte sceneHandle, Packet packet)
        {
            var scene = _scenes[sceneHandle];
            if (scene == null)
            {
                return false;
            }
            else
            {
                packet.Metadata["scene"] = scene;
                scene.HandleMessage(packet);
                return true;
            }

        }

        public void AddScene(Scene scene)
        {
            _scenes[scene.Handle] = scene;
        }

        public void RemoveScene(byte sceneHandle)
        {
            _scenes[sceneHandle] = null;
        }
    }
}
