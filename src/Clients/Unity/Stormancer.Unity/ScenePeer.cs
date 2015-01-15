using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    internal class ScenePeer : IScenePeer
    {
        private readonly IConnection _connection;
        private readonly byte _sceneHandle;
        private readonly IDictionary<string, ushort> _routeMapping;
        private readonly Scene _scene;
        public ScenePeer(IConnection connection, byte sceneHandle, IDictionary<string, ushort> routeMapping, Scene scene)
        {
            _connection = connection;
            _sceneHandle = sceneHandle;
            _routeMapping = routeMapping;
            _scene = scene;
        }
        public void Send(string route, Action<System.IO.Stream> writer, PacketPriority priority, PacketReliability reliability)
        {
            _connection.SendToScene(_sceneHandle, _routeMapping[route], writer, priority, reliability, (char)0);
        }


        public void Disconnect()
        {
            _scene.Disconnect();
        }


        public long Id
        {
            get { return _connection.Id; }
        }
    }
}
