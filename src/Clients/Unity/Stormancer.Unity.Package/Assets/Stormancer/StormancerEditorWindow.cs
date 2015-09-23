#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Stormancer;
using Stormancer.Core;
using Stormancer.EditorPlugin;

namespace Stormancer.EditorPluginWindow
{

    public class Folds
	{
		public bool client = false;
		public bool logs = false;
		public bool scene = false;
		public List<SceneFolds> scenes = new List<SceneFolds>();
	}
	
	public class SceneFolds
	{
		public bool connected = false;
		public bool scene = false;
		public bool logs = false;
		public bool routes = false;

		public List<Vector2> logs_Scrolls = new List<Vector2>();
	}
	
	public class StormancerEditorWindow : EditorWindow
	{
		static ConcurrentDictionary<string, StormancerClientViewModel> clients;
	
		private List<Vector2> scrolls_pos = new List<Vector2>();
		private List<Folds> folds = new List<Folds>();
		static private StormancerEditorWindow window;
	
		private bool initiated = false;
		
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
	
			int i = 1;
	
			scrolls_pos[0] = GUILayout.BeginScrollView(scrolls_pos[0], GUILayout.Width (window.position.width), GUILayout.Height(window.position.height));
			GUILayout.Label ("Client informations", EditorStyles.boldLabel);
			EditorGUILayout.Separator();
			EditorGUI.indentLevel++;
			if (clients == null)
				return;
			foreach (StormancerClientViewModel c in clients.Values)
			{
				EditorGUILayout.Separator();
				while (folds.Count - 1 < i)
					folds.Add (new Folds());
				folds[i].client = EditorGUILayout.Foldout (folds[i].client, "client" + i.ToString());
				if (folds[i].client == true)
				{
                    EditorGUI.indentLevel++;
					EditorGUI.indentLevel++;
					ShowScene(i, c);
					ShowClientLogs(i, c);
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
	
					EditorGUILayout.BeginHorizontal();
					folds[i].scenes[j].routes = EditorGUILayout.Foldout(folds[i].scenes[j].routes, v.scene.Id);
					EditorGUILayout.Toggle("Connected", v.connected);
					EditorGUILayout.EndHorizontal();

					if (folds[i].scenes[j].routes == true)
					{	
						EditorGUI.indentLevel++;
						foreach(string route in v.routes)
							EditorGUILayout.LabelField(route);
						EditorGUI.indentLevel--;
					}
					ShowSceneLogs(i, j, v);
					EditorGUI.indentLevel--;
					j++;
				}
				EditorGUI.indentLevel--;
			}
		}
	
		private void ShowSceneLogs(int i, int j, StormancerSceneViewModel s)
		{
			if (folds[i].scenes[j].logs_Scrolls.Count - 1 < j)
				folds[i].scenes[j].logs_Scrolls.Add (new Vector2(0,0));
			folds[i].scenes[j].logs = EditorGUILayout.Foldout(folds[i].scenes[j].logs, "Logs");
			if (folds[i].scenes[j].logs == true)
			{
				folds[i].scenes[j].logs_Scrolls[j] = EditorGUILayout.BeginScrollView(folds[i].scenes[j].logs_Scrolls[j], GUILayout.Width (window.position.width), GUILayout.Height(100));
				EditorGUILayout.BeginVertical();
				foreach (StormancerEditorLog log in s.log)
				{
					EditorGUILayout.TextArea(log.message);
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
			}
		}

		private void ShowClientLogs(int i, StormancerClientViewModel c)
		{
			if (scrolls_pos.Count - 1 < i)
				scrolls_pos.Add (new Vector2(0,0));
			folds[i].logs = EditorGUILayout.Foldout(folds[i].logs, "Logs");
			if (folds[i].logs == true)
			{
				scrolls_pos[i] = EditorGUILayout.BeginScrollView(scrolls_pos[i], GUILayout.Width (window.position.width), GUILayout.Height(100));
				EditorGUILayout.BeginVertical();
				foreach (StormancerEditorLog log in c.log)
				{
					EditorGUILayout.TextArea(log.message);
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndScrollView();
			}
		}

		private void myInit()
		{
			scrolls_pos.Add (new Vector2(0, 0));
			initiated = true;
		}
	
		void Update()
		{
			if (initiated == false)
				myInit();
			clients = StormancerEditorDataCollector.Instance.clients;
	
			//Repaint();
		}
	}
}
#endif