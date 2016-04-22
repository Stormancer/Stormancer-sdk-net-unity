#if UNITY_EDITOR
using Stormancer;
using Stormancer.Plugins;
using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace Stormancer.Plugins
{
    public class ConnectionWrapper : IConnection
    {
        private IConnection Connection;
        private Stormancer.EditorPlugin.StormancerEditorPlugin _plugin;

        public long Id { get { return Connection.Id; } }
        public string IpAddress { get { return Connection.IpAddress; } }
        public DateTime ConnectionDate { get { return Connection.ConnectionDate; } }
        public Dictionary<string, string> Metadata { get { return Connection.Metadata; } }
        public string Account { get { return Connection.Account; } }
        public string Application { get { return Connection.Application; } }
        public ConnectionState State { get { return Connection.State; } }
        public Action<string> ConnectionClosed { get { return Connection.ConnectionClosed; } set { Connection.ConnectionClosed = value; } }
        public int Ping { get { return Connection.Ping; } }

        public void RegisterComponent<T>(T component)
        {
            Connection.RegisterComponent<T>(component);
        }

        public T GetComponent<T>()
        {
            return Connection.GetComponent<T>();
        }

        public void Close()
        {
            Connection.Close();
        }

        public void SendSystem(byte msgId, Action<Stream> writer)
        {
            Connection.SendSystem(msgId, s =>
            {
                var stream = new OutputLogStream(s, _plugin._clientVM);
                writer(stream);
                stream.Log("system", "system");
            });
        }

        public void SendSystem(byte msgId, Action<Stream> writer, PacketPriority priority)
        {
            Connection.SendSystem(msgId, s =>
            {
                var stream = new OutputLogStream(s, _plugin._clientVM);
                writer(stream);
                stream.Log("system", "system");
            }, priority);
        }

        public void SendToScene(byte sceneIndex, ushort route, Action<Stream> writer, PacketPriority priority, PacketReliability reliability)
        {
            Connection.SendToScene(sceneIndex, route, s =>
            {
                var stream = new OutputLogStream(s, _plugin._clientVM);
                writer(stream);
                stream.Log(sceneIndex, route);
            }, priority, reliability);
        }

        public void SetApplication(string account, string application)
        {
            Connection.SetApplication(account, application);
        }

        public IConnectionStatistics GetConnectionStatistics()
        {
            return Connection.GetConnectionStatistics();
        }

        public ConnectionWrapper(IConnection c, Stormancer.EditorPlugin.StormancerEditorPlugin plugin)
        {
            Connection = c;
            _plugin = plugin;
        }
    }
}
#endif