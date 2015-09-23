using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Stormancer;
using Stormancer.Core;

namespace Stormancer.EditorPlugin
{
	public class StormancerClientViewModel
	{
		public Client client;
		public ConcurrentDictionary<string, StormancerSceneViewModel> scenes = new ConcurrentDictionary<string, StormancerSceneViewModel>();
		public ConcurrentStack<StormancerEditorLog> log = new ConcurrentStack<StormancerEditorLog>();

		public StormancerClientViewModel(Client clt)
		{
			client = clt;
		}
	}

	public class StormancerSceneViewModel
	{
		public Scene scene;
		public bool connected = false;
		public ConcurrentQueue<string> routes = new ConcurrentQueue<string>();
		public ConcurrentStack<StormancerEditorLog> log = new ConcurrentStack<StormancerEditorLog>();

		public StormancerSceneViewModel(Scene scn)
		{
			scene = scn;
		}
	}

	public struct StormancerEditorLog
	{
		public string logLevel;
		public string message;
	}

	public class StormancerEditorDataCollector
	{
		private static StormancerEditorDataCollector _instance;
		public static StormancerEditorDataCollector Instance
		{
			get
			{
				if (_instance == null)
					_instance = new StormancerEditorDataCollector();
				return _instance;
			}
		}

		public ConcurrentDictionary<string, StormancerClientViewModel> clients = new ConcurrentDictionary<string, StormancerClientViewModel>();

	}
}