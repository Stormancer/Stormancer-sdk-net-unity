using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This class shows some of the features of the charts package.
/// </summary>
public class AnalyticsWindow : EditorWindow , ClickResponder {
	
	private bool foldoutTotalPlays = false;
	private bool foldoutTopCountries = false;
	private bool foldOutStartsVsDeaths = false;
	
	BarChart chartTotalPlays;
	PieChart chartTopCountries;
	LineChart chartLevelEngagement;
	
 	GUIStyle boxStyle;
	
    [MenuItem ("Window/Analytics Sample")]
    static void Init () {
        // Get existing open window or if none, make a new one:
		AnalyticsWindow window = (AnalyticsWindow) EditorWindow.GetWindow (typeof (AnalyticsWindow));
		
		// Listen for updates - use this if you want real time graphs
		// if (EditorApplication.update != window.EditorUpdate) EditorApplication.update += window.EditorUpdate;
		
		// Listen for moves
		window.wantsMouseMove = true;
	
		// Create a style which loads a background image
		window.boxStyle = new GUIStyle();
		window.boxStyle.normal.background = (Texture2D) Resources.Load("Border", typeof(Texture2D));
		window.boxStyle.border = new RectOffset(32,32,32,32);
		window.boxStyle.margin = new RectOffset(4,4,4,4);

		window.Focus();
    }
	
   void OnGUI () {
		foldoutTotalPlays = EditorGUILayout.Foldout(foldoutTotalPlays, "Total Plays");
		if (foldoutTotalPlays) {
			
			if (chartTotalPlays == null) {
				chartTotalPlays = new BarChart(this, 200.0f);
			}
			chartTotalPlays.data = new List<float[]>(){new float[]{26f}, new float[]{12f, 6f, 1f}, new float[]{6f}, new float[]{0f}, new float[]{5f}, new float[]{10f,5f}, new float[]{8f,8f}};
			// You can also use the set data method if you just have a list of floats
			//chartTotalPlays.SetData(new List<float>(){26f, 12f, 6f, 0f, 5f, 10f, 1f});
			chartTotalPlays.labels = new List<string>(){"23 Feb", "24 Feb", "25 Feb", "26 Feb", "27 Feb", "28 Feb", "29 Feb"};
			// If depth is bigger than 0 the chart will be 3D
			chartTotalPlays.depth = 7.0f;
			// Send click events to this window
			chartTotalPlays.clickResponder = this;
			chartTotalPlays.axisColor = Color.black;
			chartTotalPlays.fontColor = Color.black;
			chartTotalPlays.boxStyle = boxStyle;
			// Do the drawing
			chartTotalPlays.DrawChart();
			
		}
		
		foldoutTopCountries = EditorGUILayout.Foldout(foldoutTopCountries, "Top Countries");
		if (foldoutTopCountries){
			if (chartTopCountries == null) chartTopCountries = new PieChart(this, 300.0f);
			chartTopCountries.data = new List<float>(){25.0f, 9.0f, 6.0f, 13.0f};
			chartTopCountries.labels = new List<string>(){"US","China", "Aus", "UK"};
			// User percentage not value as the default value
			chartTopCountries.showValuesAsPercent = true;
			chartTopCountries.formatString = "({0:0}%)";
			// Use a different format string for the selected arc
			chartTopCountries.selectedFormatString = "{0:0} Users";
			// Always show values
			chartTopCountries.valueViewMode = ViewMode.ALWAYS;
			// Always show labels
			chartTopCountries.labelViewMode = ViewMode.ALWAYS;
			chartTopCountries.clickResponder = this;
			// Draw the chart
			chartTopCountries.DrawChart();
		}
		
		foldOutStartsVsDeaths = EditorGUILayout.Foldout(foldOutStartsVsDeaths, "Level Engagement");
		if (foldOutStartsVsDeaths) {
			if (chartLevelEngagement == null) {
				chartLevelEngagement = new LineChart(this, 200.0f);
			}
			// Set up three lines
			chartLevelEngagement.data = new List<float>[] { new List<float>(){126.5f, 92.5f, 85f, 75f, 85f, 55.0f, 41.0f}, 
															new List<float>(){12.5f, 75f, 100f, 85f, 25.0f, 141.0f},
														    new List<float>(){106.5f, 122.5f, 65f, 100f, 85f, 35.0f, 141.0f}};
			chartLevelEngagement.dataLabels = new List<string> {"Level 1", "Level 2", "Level 3"};
			chartLevelEngagement.axisLabels = new List<string> {"23 Feb", "24 Feb", "25 Feb", "26 Feb", "27 Feb", "28 Feb", "29 Feb"};
			// Don't draw gridlines
			chartLevelEngagement.gridLines = 0;
			// Draw the chart
			chartLevelEngagement.DrawChart();
		}

	}
	
	public void Click(object chart, string label, float value) {
		Debug.Log("Clicked on " + chart.GetType() + " with label " + label + " and value: " + value);	
	}
	
	// Use this if you want real time updating
	void EditorUpdate() {
		Repaint();
	}
	
}

 