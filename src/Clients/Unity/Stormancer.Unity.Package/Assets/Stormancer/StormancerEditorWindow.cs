#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using Stormancer;
using Stormancer.Core;
using Stormancer.EditorPlugin;

namespace Stormancer.EditorPluginWindow
{

    internal class Folds
	{
		public bool client = false;
		public bool logs = false;
		public bool scene = false;
		public List<SceneFolds> scenes = new List<SceneFolds>();
	}
	
	internal class SceneFolds
	{
		public bool connected = false;
		public bool scene = false;
		public bool logs = false;
		public bool routes = false;
        public bool serverRoutes = false;
        public bool localRoutes = false;
	}
	
	public class StormancerEditorWindow : EditorWindow
	{
		static ConcurrentDictionary<string, StormancerClientViewModel> clients;
	
		private Vector2 scroll_pos = new Vector2(0, 0);
        private Vector2 log_scroll = new Vector2(0, 0);

        private List<Folds> folds = new List<Folds>();
		static private StormancerEditorWindow window;
	
		private bool initiated = false;
        private StormancerEditorLogViewModel logsToShow = null;
        private StormancerRouteViewModel routeToShow = null;
        private LineChart chart;
        private float ChartSlider;
		
		[MenuItem ("Window/Stormancer Editor Window")]
		static void Init ()
		{
			window = (StormancerEditorWindow)EditorWindow.GetWindow (typeof (StormancerEditorWindow));
			window.Show();
        }
		
		void OnGUI ()
		{
    		ShowStormancerDebug ();
		}
	
		void ShowStormancerDebug()
		{
			if (window == null)
				window = (StormancerEditorWindow)EditorWindow.GetWindow (typeof (StormancerEditorWindow));
			if (initiated == false)
					return;
	
           if (logsToShow != null)
            {
                ShowLogs();
            }
           else if (routeToShow != null)
            {
                ShowChart();
            }
            else
            {
                ShowClients();
            }
		}
	
        private void ShowClients()
        {
            int i = 1;

            GUILayout.Label("Client informations", EditorStyles.boldLabel);
            EditorGUILayout.Separator();
            scroll_pos = GUILayout.BeginScrollView(scroll_pos, GUILayout.Width(window.position.width), GUILayout.Height(window.position.height - (window.position.height / 10)));

            EditorGUI.indentLevel++;
            if (clients == null)
                return;
            foreach (StormancerClientViewModel c in clients.Values)
            {
                EditorGUILayout.Separator();
                while (folds.Count - 1 < i)
                    folds.Add(new Folds());
                GUILayout.BeginHorizontal(GUILayout.Width(200), GUILayout.Height(20), GUILayout.MinWidth(200), GUILayout.MaxWidth(200));

                folds[i].client = EditorGUILayout.Foldout(folds[i].client, "client" + i.ToString());
                if (GUILayout.Button("show logs", GUILayout.Width(70)) && logsToShow == null)
                {
                    logsToShow = c.log;
                    log_scroll = Vector2.zero;
                }
                if (c.exportLogs == false && GUILayout.Button("record", GUILayout.Width(50)))
                {
                    c.dateOfRecord = DateTime.UtcNow.ToString("yyyyMMdd-HH-mm-ss");
                    c.exportLogs = true;
                }
                else if (c.exportLogs == true && GUILayout.Button("stop", GUILayout.Width(50)))
                {
                    c.exportLogs = false;
                }

                GUILayout.EndHorizontal();

                if (folds[i].client == true)
                {
                    EditorGUI.indentLevel++;
                    ShowScene(i, c);
                    EditorGUI.indentLevel--;
                }
                i++;
            }
            GUILayout.EndScrollView();
        }

        private void ShowScene(int i, StormancerClientViewModel c)
		{
			int j = 0;

			folds[i].scene = EditorGUILayout.Foldout(folds[i].scene, "scenes");
			if (folds[i].scene)
			{
				EditorGUI.indentLevel++;
			foreach(StormancerSceneViewModel v in c.scenes.Values)	
				{
					if (folds[i].scenes.Count - 1 < j)
						folds[i].scenes.Add(new SceneFolds());
					EditorGUI.indentLevel++;

                    EditorGUILayout.BeginHorizontal(GUILayout.Width(200), GUILayout.Height(20), GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
    				folds[i].scenes[j].routes = EditorGUILayout.Foldout(folds[i].scenes[j].routes, v.scene.Id);
                    //EditorGUILayout.Toggle("        ", v.connected);
                    if (GUILayout.Button("show logs", GUILayout.Width(100)) && logsToShow == null)
                    {
                        logsToShow = v.log;
                        log_scroll = Vector2.zero;
                    }
                    EditorGUILayout.EndHorizontal();

					if (folds[i].scenes[j].routes == true)
                    {
						EditorGUI.indentLevel++;
                        folds[i].scenes[j].serverRoutes = EditorGUILayout.Foldout(folds[i].scenes[j].serverRoutes, "server routes");
                        if (folds[i].scenes[j].serverRoutes == true)
                        {
                            EditorGUI.indentLevel++;

                            foreach (StormancerRouteViewModel route in v.remotes.Values.OrderBy(r=>r.Name))
                            {
                                GUILayout.BeginHorizontal(GUILayout.Width(400), GUILayout.Height(20), GUILayout.MinWidth(400), GUILayout.MaxWidth(window.position.width / 4));
                                EditorGUILayout.LabelField(route.Name + "     " + route.debit.ToString() + " b/s");
                                if (GUILayout.Button("Show Chart", GUILayout.Width(90)))
                                {
                                    routeToShow = route;
                                }
                                //EditorGUILayout.CurveField(route.curve);
                                GUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
                        folds[i].scenes[j].localRoutes = EditorGUILayout.Foldout(folds[i].scenes[j].localRoutes, "local routes");
                        if (folds[i].scenes[j].localRoutes == true)
                        {
                            EditorGUI.indentLevel++;
                            foreach (StormancerRouteViewModel route in v.routes.Values.OrderBy(r => r.Name))
                            {
                                GUILayout.BeginHorizontal(GUILayout.Width(300), GUILayout.Height(20), GUILayout.MinWidth(150), GUILayout.MaxWidth(400));
                                EditorGUILayout.LabelField(route.Name + "     " + route.debit.ToString() + " b/s");
                                if (GUILayout.Button("Show Chart", GUILayout.Width(90)))
                                {
                                    routeToShow = route;
                                }
                                //EditorGUILayout.CurveField(route.curve);
                                GUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
						EditorGUI.indentLevel--;
					}
					EditorGUI.indentLevel--;
					j++;
				}
				EditorGUI.indentLevel--;
			}
		}

        private void ShowChart()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(200), GUILayout.Height(20), GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
            if (GUILayout.Button("back", GUILayout.Width(80), GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.MaxWidth(160)))
            {
                routeToShow = null;
                return;
            }
            if (GUILayout.Button("clear", GUILayout.Width(80), GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.MaxWidth(160)))
            {
                while (routeToShow.averageSizeChart.Count > 0)
                {
                    routeToShow.dataChart.RemoveAt(0);
                    routeToShow.averageSizeChart.RemoveAt(0);
                    routeToShow.messageNbrChart.RemoveAt(0);
                }
            }
            if (GUILayout.Button("Export", GUILayout.Width(80), GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.MaxWidth(160)))
            {
                int i = 0;
                while (System.IO.File.Exists("Export" + i.ToString() + ".csv"))
                    i++;
                var file = System.IO.File.CreateText("Export" + i.ToString() + ".csv");
                i = 0;
                while (i < routeToShow.averageSizeChart.Count)
                {
                    file.WriteLine(string.Join(";", new string[] { routeToShow.dataChart[i].ToString(), routeToShow.averageSizeChart[i].ToString(), routeToShow.messageNbrChart[i].ToString() }));
                    i++;
                }
            }
            GUILayout.EndHorizontal();

            ChartSlider = GUILayout.HorizontalSlider(ChartSlider, 0, routeToShow.dataChart.Count - 1);
            if (ChartSlider >= routeToShow.dataChart.Count - 2)
                ChartSlider = routeToShow.dataChart.Count - 1;

            if (routeToShow.dataChart.Count > 0)
            {
                int count = 60;
                int pos = (int)ChartSlider;
                calcRange(routeToShow.dataChart.Count - 1, ref pos, ref count);

                if (chart == null)
                    chart = new LineChart(window, window.position.height - window.position.height / 10);

                chart.data = new List<float>[3];
                chart.data[0] = routeToShow.dataChart.GetRange(pos, count);
                chart.data[1] = routeToShow.averageSizeChart.GetRange(pos, count);
                chart.data[2] = routeToShow.messageNbrChart.GetRange(pos, count);

                chart.dataLabels = new List<string> { "bytes per seconds", "average packet size", "number of messages" };
                chart.axisLabels = new List<string> { "0" };
                chart.DrawChart();
            }
        }

		private void ShowLogs()
		{
           GUILayout.BeginHorizontal();
            if (GUILayout.Button("back", GUILayout.Width(80), GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.MaxWidth(160)))
            {
                logsToShow = null;
                return;
            }
            if (GUILayout.Button("clear", GUILayout.Width(80), GUILayout.Height(20), GUILayout.MinWidth(40), GUILayout.MaxWidth(160)))
                logsToShow.Clear();
            GUILayout.EndHorizontal();
			log_scroll = EditorGUILayout.BeginScrollView(log_scroll, GUILayout.Width (window.position.width), GUILayout.Height(window.position.height));
            GUILayout.Label("");
            foreach (StormancerEditorLog log in logsToShow.log)
			{
                EditorGUILayout.BeginVertical(GUILayout.Width(window.position.width), GUILayout.Height(20));
                EditorGUILayout.SelectableLabel(log.logLevel + ": " + log.message);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
		}

        private void calcRange(int max, ref int pos, ref int count)
        {
            if (max < 0)
                max = 0;
            if (count > max)
                count = max;
            if (pos + count > max)
                pos =  pos - ((pos + count) - max);
        }

		private void myInit()
		{
            initiated = true;
            clients = StormancerEditorDataCollector.Instance.clients;
        }

        private void UpdateRouteData(StormancerClientViewModel clientVM, StormancerRouteViewModel route)
        {
            if (route.dataChart.Count >= 3600)
                route.dataChart.RemoveAt(0);
            if (route.messageNbrChart.Count >= 3600)
                route.messageNbrChart.RemoveAt(0);
            if (route.averageSizeChart.Count >= 3600)
                route.averageSizeChart.RemoveAt(0);
            route.debit = route.sizeStack;
            route.curve.AddKey(clientVM.client.Clock, route.sizeStack);
            route.dataChart.Add(route.sizeStack);
            route.messageNbrChart.Add(route.messageNbr);
            route.averageSizeChart.Add(route.averageSize);

            route.messageNbr = 0;
            route.averageSize = 0;
            route.sizeStack = 0;
        }

        void Update()
		{
			if (initiated == false)
				myInit();
            clients = StormancerEditorDataCollector.Instance.clients;
            foreach (StormancerClientViewModel c in clients.Values)
            {
                c.WriteLog();
                if (c.lastUpdate + 1000 < c.client.Clock)
                {
                    c.lastUpdate = c.client.Clock;
                    foreach (StormancerSceneViewModel s in c.scenes.Values)
                    {
                        if (s.scene.Connected == true)
                        {
                            foreach (StormancerRouteViewModel r in s.routes.Values)
                            {
                                UpdateRouteData(c, r);
                            }
                            foreach (StormancerRouteViewModel r in s.remotes.Values)
                            {
                                UpdateRouteData(c, r);
                            }
                        }
                    }
                }
            }
	
			Repaint();
		}
	}
}
#endif