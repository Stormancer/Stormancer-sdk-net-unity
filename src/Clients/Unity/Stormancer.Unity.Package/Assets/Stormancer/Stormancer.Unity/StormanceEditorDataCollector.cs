using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Stormancer;
using Stormancer.Core;
using System.IO;
using System;
using System.Linq;

namespace Stormancer.EditorPlugin
{
    public class StormancerClientViewModel
    {
        public string id;
        public Client client;
        public long lastUpdate = 0;
        public ConcurrentDictionary<string, StormancerSceneViewModel> scenes = new ConcurrentDictionary<string, StormancerSceneViewModel>();
        public StormancerEditorLogViewModel log = new StormancerEditorLogViewModel();
        public bool exportLogs = false;
        public string dateOfRecord = "";
        private ConcurrentQueue<string> _messageExport = new ConcurrentQueue<string>();

        public StormancerClientViewModel(Client clt)
        {
            client = clt;
        }

        public void WritePacketLog(bool isPacketIncoming, string scene, string route, IEnumerable<byte> data)
        {
            var now = DateTime.UtcNow;

            var bytes = String.Join(" ", data.Select(b => b.ToString("X2")).ToArray());

            var message = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
               now.ToString("yyyy-MM dd:HH:mm:ss.fff"),
               client.Clock.ToString(),
               isPacketIncoming ? "R" : "S",
               scene,
               route,
               bytes
                );

            _messageExport.Enqueue(message);
        }

        public void WriteLog()
        {
            if (_messageExport.Count != 0)
            {
                using (var file = new StreamWriter("Message_Logs_" + dateOfRecord + "_" + id + ".log", true))
                {
                    string logEntry;
                    while (_messageExport.TryDequeue(out logEntry))
                    {
                        file.WriteLine(logEntry);
                    }
                }
            }
        }
    }

    public class StormancerSceneViewModel
    {
        public Scene scene;
        public bool connected = false;
        public ConcurrentDictionary<string, StormancerRouteViewModel> routes = new ConcurrentDictionary<string, StormancerRouteViewModel>();
        public ConcurrentDictionary<string, StormancerRouteViewModel> remotes = new ConcurrentDictionary<string, StormancerRouteViewModel>();
        public StormancerEditorLogViewModel log = new StormancerEditorLogViewModel();

        public StormancerSceneViewModel(Scene scn)
        {
            scene = scn;
            foreach(Route r in scn.RemoteRoutes)
            {
                this.remotes.TryAdd(r.Name, new StormancerRouteViewModel(r));
            }
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
        public ConcurrentDictionary<string, StormancerClientViewModel> clients = new ConcurrentDictionary<string, StormancerClientViewModel>();

        public void GetDataStatistics(Stream stream, StormancerClientViewModel clientVM, StormancerRouteViewModel route)
        {
            route.sizeStack += stream.Length;
            route.messageNbr += 1;
            if (route.averageSizeChart.Count == 0)
                route.averageSize = stream.Length;
            else
                route.averageSize = (route.averageSize * (route.messageNbr - 1) + stream.Length) / route.messageNbr;
        }
    }
}