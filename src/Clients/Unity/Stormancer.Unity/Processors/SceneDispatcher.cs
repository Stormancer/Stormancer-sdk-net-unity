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
        private Scene[] _scenes = new Scene[(int)byte.MaxValue - (int)MessageIDTypes.ID_SCENES + 1];

        public void RegisterProcessor(PacketProcessorConfig config)
        {
            config.AddCatchAllProcessor(Handler);
        }

        private bool Handler(byte sceneHandle, Packet packet)
        {
            if (sceneHandle < (byte)MessageIDTypes.ID_SCENES)
            {
                return false;
            }
            var scene = _scenes[sceneHandle - (byte)MessageIDTypes.ID_SCENES];
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
            _scenes[scene.Handle - (byte)MessageIDTypes.ID_SCENES] = scene;
        }

        public void RemoveScene(byte sceneHandle)
        {
            _scenes[sceneHandle - (byte)MessageIDTypes.ID_SCENES] = null;
        }
    }
}
