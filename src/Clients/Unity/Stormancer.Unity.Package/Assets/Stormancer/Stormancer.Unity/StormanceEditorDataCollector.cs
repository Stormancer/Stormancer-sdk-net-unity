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
		public StormancerEditorLogViewModel log = new StormancerEditorLogViewModel();

		public StormancerClientViewModel(Client clt)
		{
			client = clt;
		}
	}

	public class StormancerSceneViewModel
	{
		public Scene scene;
		public bool connected = false;
		public ConcurrentDictionary<string, StormancerRouteViewModel> routes = new ConcurrentDictionary<string, StormancerRouteViewModel>();
		public StormancerEditorLogViewModel log = new StormancerEditorLogViewModel();

		public StormancerSceneViewModel(Scene scn)
		{
			scene = scn;
		}
	}

    public class StormancerRouteViewModel
    {
        private Route _route;
        public string Name
        {
            get
            {
                return _route.Name;
            }
        }
        public ushort Handle
        {
            get
            {
                return _route.Handle;
            }
        }

        public AnimationCurve curve = new AnimationCurve();
        public List<float> dataChart = new List<float>();
        public List<float> averageSizeChart = new List<float>();
        public List<float> messageNbrChart = new List<float>();
        public float debit;
        public float sizeStack;
        public float messageNbr;
        public float averageSize;
        public long lastUpdate = 0;

        public StormancerRouteViewModel(Route route)
        {
            _route = route;
        }
    }

	public struct StormancerEditorLog
	{
		public string logLevel;
		public string message;
	}
    
    public class StormancerEditorLogViewModel
    {
        public ConcurrentQueue<StormancerEditorLog> log = new ConcurrentQueue<StormancerEditorLog>();

        public void Clear()
        {
            StormancerEditorLog temp;

            while (log.IsEmpty == false)
                log.TryDequeue(out temp);
        }
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

        public System.IO.StreamWriter temp_csv = System.IO.File.CreateText("temp.csv");
        public ConcurrentDictionary<string, StormancerClientViewModel> clients = new ConcurrentDictionary<string, StormancerClientViewModel>();

	}
}