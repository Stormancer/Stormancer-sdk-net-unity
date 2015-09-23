using System;
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

		private string _id = Guid.NewGuid().ToString();
		private StormancerClientViewModel _clientVM;

		public void Build(PluginBuildContext ctx)
		{

			ctx.ClientCreated += client =>
			{
				var innerLoggerFactory = client.GetComponentFactory<ILogger>();
				_clientVM = new StormancerClientViewModel(client);
				client.RegisterComponent<ILogger>(()=> new InterceptorLogger(innerLoggerFactory(), _clientVM));
				StormancerEditorDataCollector.Instance.clients.TryAdd(_id, _clientVM);
			};

			ctx.ClientDestroyed += client =>
			{
				StormancerClientViewModel temp;
				StormancerEditorDataCollector.Instance.clients.TryRemove(_id, out temp);
			};

			ctx.SceneCreated +=  scene =>
			{
				StormancerEditorDataCollector.Instance.clients[_id].scenes.TryAdd(scene.Id, new StormancerSceneViewModel(scene));
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

			ctx.RouteCreated += (scene, route) =>
			{
				if (StormancerEditorDataCollector.Instance.clients.ContainsKey(_id))
				{
					if (StormancerEditorDataCollector.Instance.clients[_id].scenes.ContainsKey(scene.Id))
						StormancerEditorDataCollector.Instance.clients[_id].scenes[scene.Id].routes.Enqueue(route);
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
			public void Log(string logLevel, string category, string message, object context = null)
			{
				var temp = new StormancerEditorLog();
				temp.logLevel = logLevel;
				temp.message = message;
				_clientVM.log.Push(temp);
				foreach (StormancerSceneViewModel s in _clientVM.scenes.Values)
				{
					if (category == s.scene.Id)
						s.log.Push(temp);
				}
				_innerLogger.Log(logLevel, category, message, context);
			}

			public void Trace (string message, params object[] p)
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
				_clientVM.log.Push(temp);
				_innerLogger.Trace(message,p);
			}

			public void Debug (string message, params object[] p)
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
                _clientVM.log.Push(temp);
				_innerLogger.Debug(message,p);

			}
			public void Error (Exception ex)
			{
				var temp = new StormancerEditorLog();
				temp.logLevel = "Error";
				temp.message = ex.Message;
				_clientVM.log.Push(temp);
				_innerLogger.Error(ex);

			}
			public void Error (string message, params object[] p)
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
                _clientVM.log.Push(temp);
				_innerLogger.Error(message,p);

			}
			public void Info (string message, params object[] p)
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
                _clientVM.log.Push(temp);
				_innerLogger.Info(message,p);
		
			}
			#endregion
		}

	}


}