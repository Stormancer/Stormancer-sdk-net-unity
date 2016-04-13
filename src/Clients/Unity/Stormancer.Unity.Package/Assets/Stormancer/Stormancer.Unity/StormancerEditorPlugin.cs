using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;

namespace Stormancer.EditorPlugin
{
    public class StormancerEditorPlugin : IClientPlugin
    {

        public string _id = Guid.NewGuid().ToString();
        public StormancerClientViewModel _clientVM;


        public void Build(PluginBuildContext ctx)
        {

            ctx.ClientCreated += client =>
            {
                var innerLoggerFactory = client.DependencyResolver.Resolve<ILogger>();
                _clientVM = new StormancerClientViewModel(client);
                _clientVM.id = _id;
                client.DependencyResolver.Register<ILogger>(() => new InterceptorLogger(innerLoggerFactory, _clientVM));
                StormancerEditorDataCollector.Instance.clients.TryAdd(_id, _clientVM);
            };

            ctx.ClientDestroyed += client =>
            {
                StormancerClientViewModel temp;
                StormancerEditorDataCollector.Instance.clients.TryRemove(_id, out temp);
            };

            ctx.SceneCreated += scene =>
           {
               _clientVM.scenes.TryAdd(scene.Id, new StormancerSceneViewModel(scene));
           };

            ctx.SceneConnected += scene =>
            {
                if (StormancerEditorDataCollector.Instance.clients.ContainsKey(_id))
                {
                    if (StormancerEditorDataCollector.Instance.clients[_id].scenes.ContainsKey(scene.Id))
                        StormancerEditorDataCollector.Instance.clients[_id].scenes[scene.Id].connected = true;
                }
            };

            ctx.SceneDisconnected += scene =>
            {
                if (StormancerEditorDataCollector.Instance.clients.ContainsKey(_id))
                {
                    if (StormancerEditorDataCollector.Instance.clients[_id].scenes.ContainsKey(scene.Id))
                        StormancerEditorDataCollector.Instance.clients[_id].scenes[scene.Id].connected = false;
                }
            };

            ctx.RouteCreated += (Scene scene, Route route) =>
            {
                StormancerSceneViewModel sceneVM;
                if (_clientVM.scenes.TryGetValue(scene.Id, out sceneVM))
                {
                    sceneVM.routes.TryAdd(route.Name, new StormancerRouteViewModel(route));
                }
            };

            ctx.PacketReceived += (Packet packet) =>
            {
                if (_clientVM != null && _clientVM.exportLogs == true)
                {
                    var pos = packet.Stream.Position;
                    var message = new List<byte>();
                    var buffer = new byte[1024];

                    int readByte = 0;
                    do
                    {
                        readByte = packet.Stream.Read(buffer, 0, 1024);
                        message.AddRange(buffer.Take(readByte));
                    }
                    while (readByte > 0);
                    packet.Stream.Seek(pos, SeekOrigin.Begin);

                    var scene = (Scene)packet.Metadata["scene"];
                    var routeId = (ushort)packet.Metadata["routeId"];

                    StormancerRouteViewModel routeVM = null;
                    StormancerSceneViewModel sceneVM = null;
                    string route = "";
                    if (_clientVM.scenes.TryGetValue(scene.Id, out sceneVM))
                        routeVM = sceneVM.routes.Values.FirstOrDefault(r => r.Handle == routeId);
                    if (routeVM != null)
                        route = routeVM.Name;
                    if (route == "")
                        route = "system";
                    _clientVM.WritePacketLog(true, scene.Id, route, message);
                }
            };

            ctx.PacketReceived += (Packet packet) =>
            {
                if (packet.Metadata.ContainsKey("scene"))
                {
                    var scene = (Scene)packet.Metadata["scene"];
                    StormancerSceneViewModel sceneVM;
                    if (_clientVM.scenes.TryGetValue(scene.Id, out sceneVM))
                    {
                        var routeId = (ushort)packet.Metadata["routeId"];
                        var temp = sceneVM.routes.Values.FirstOrDefault(r => r.Handle == routeId);
                        if (temp != null)
                        {
                            StormancerEditorDataCollector.Instance.GetDataStatistics(packet.Stream, _clientVM, temp);
                        }
                    }
                }
            };
        }

        private class InterceptorLogger : ILogger
        {
            private readonly ILogger _innerLogger;
            private readonly StormancerClientViewModel _clientVM;
            public InterceptorLogger(ILogger innerLogger, StormancerClientViewModel client)
            {
                _innerLogger = innerLogger;
                _clientVM = client;
            }


            #region ILogger implementation
            public void Log(Stormancer.Diagnostics.LogLevel logLevel, string category, string message, object context = null)
            {
                var temp = new StormancerEditorLog();
                temp.logLevel = logLevel.ToString();
                temp.message = message;
                _clientVM.log.log.Enqueue(temp);
                foreach (StormancerSceneViewModel s in _clientVM.scenes.Values)
                {
                    if (category == s.scene.Id)
                        s.log.log.Enqueue(temp);
                }
                _innerLogger.Log(logLevel, category, message, context);
            }

            public void Trace(string message, params object[] p)
            {
                var temp = new StormancerEditorLog();
                temp.logLevel = "trace";
                try
                {
                    temp.message = string.Format(message, p);
                }
                catch (Exception ex)
                {
                    Error(ex);
                }
                _clientVM.log.log.Enqueue(temp);
                _innerLogger.Trace(message, p);
            }

            public void Debug(string message, params object[] p)
            {
                var temp = new StormancerEditorLog();
                temp.logLevel = "Debug";
                try
                {
                    temp.message = string.Format(message, p);
                }
                catch (Exception ex)
                {
                    Error(ex);
                }
                _clientVM.log.log.Enqueue(temp);
                _innerLogger.Debug(message, p);

            }

            public void Error(Exception ex)
            {
                var temp = new StormancerEditorLog();
                temp.logLevel = "Error";
                temp.message = ex.Message;
                _clientVM.log.log.Enqueue(temp);
                _innerLogger.Error(ex);

            }

            public void Error(string message, params object[] p)
            {
                var temp = new StormancerEditorLog();
                temp.logLevel = "Error";
                try
                {
                    temp.message = string.Format(message, p);
                }
                catch (Exception ex)
                {
                    Error(ex);
                }
                _clientVM.log.log.Enqueue(temp);
                _innerLogger.Error(message, p);

            }
            public void Info(string message, params object[] p)
            {
                var temp = new StormancerEditorLog();
                temp.logLevel = "Info";
                try
                {
                    temp.message = string.Format(message, p);
                }
                catch (Exception ex)
                {
                    Error(ex);
                }
                _clientVM.log.log.Enqueue(temp);
                _innerLogger.Info(message, p);

            }
            #endregion
        }
    }
}