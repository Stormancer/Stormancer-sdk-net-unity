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
        private readonly IDictionary<string, Route> _routeMapping;
        private readonly Scene _scene;
        public ScenePeer(IConnection connection, byte sceneHandle, IDictionary<string, Route> routeMapping, Scene scene)
        {
            _connection = connection;
            _sceneHandle = sceneHandle;
            _routeMapping = routeMapping;
            _scene = scene;
        }
        public void Send(string route, Action<System.IO.Stream> writer, PacketPriority priority, PacketReliability reliability)
        {
            Route r;
            if (!_routeMapping.TryGetValue(route, out r))
            {
                throw new ArgumentException(string.Format("The route '{0}' is not declared on the server.", route));
            }
            _connection.SendToScene(_sceneHandle, r.Index, writer, priority, reliability);
        }


        public void Disconnect()
        {
            _scene.Disconnect();
        }


        public long Id
        {
            get { return _connection.Id; }
        }

        private Dictionary<Type, object> _components = new Dictionary<Type, object>();
        public T GetComponent<T>()
        {
            object result;
            if (!_components.TryGetValue(typeof(T), out result))
            {
                return _connection.GetComponent<T>();
            }
            return default(T);
        }
    }
}
