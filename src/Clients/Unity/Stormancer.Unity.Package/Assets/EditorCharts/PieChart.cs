using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PieChart 
{
	
	
	
	/// <summary>
	/// The data driving the chart.
	/// </summary>
   	public List <float> data;
	
	/// <summary>
	/// The labels of the data items.
	/// </summary>
	public List <string> labels;
	
	/// <summary>
	/// The distance between each bar.
	/// </summary>
	public float barSpacer = 0.5f;
	
	/// <summary>
	/// The x border.
	/// </summary>
	public float xBorder = 30;
	
	/// <summary>
	/// The y border.
	/// </summary>
	public float yBorder = 20;
	
	/// <summary>
	/// The format string for the values.
	/// </summary>
	public string formatString = "{0:0}";
	
	/// <summary>
	/// The format string for the axis values.
	/// </summary>
	public string axisFormatString = "{0:0}";
	
	/// <summary>
	/// The colors to draw the bar in. Colors will cycle if there aren't enough to draw each stack. The first
	/// color will be used by a single bar chart.
	/// </summary>
	public List<Color> colors = new List<Color>{Colors.PastelYellow, Colors.PastelPurple, Colors.PastelGreen, Colors.PastelBlue};
	
	/// <summary>
	/// The color of the selected bar.
	/// </summary>
	public Color selectedColor = Colors.PastelOrange;
	
	/// <summary>
	/// The depth of the bars, larger than zero will make this a 3D bar graph.
	/// </summary>
	public float depth = 0.0f;
	
	/// <summary>
	/// The value view mode. Do we see the values, NEVER, ALWAYS or only ON_SELECT.
	/// </summary>
	public ViewMode valueViewMode = ViewMode.ON_SELECT;
	
	
	/// <summary>
	/// The label view mode. Do we see the labels, NEVER, ALWAYS or only ON_SELECT.
	/// </summary>
	public ViewMode labelViewMode = ViewMode.ALWAYS;
	
	/// <summary>
	/// How many horizontal grid lines to draw.
	/// </summary>
	public int gridLines = 4;
	
	/// <summary>
	/// What level should values on the axis be rounded too?
	/// </summary>
	public float axisRounding = 10f;
	/// <summary>
	/// The ClickReponder that recieves click events.
	/// </summary>
	public ClickResponder clickResponder;
	
	/// <summary>
	/// The color of the axis.
	/// </summary>
	public Color axisColor = Color.white;
	
	/// <summary>
	/// The color of the label and value font.
	/// </summary>
	public Color fontColor = Color.white;
	
	/// <summary>
	/// Additional GUI style drawn in the background. Use this to add a backgroudn image or background box.
	/// </summary>
	public GUIStyle boxStyle;
	
	/// <summary>
	/// Format string used for the selected value.
	/// </summary>
	public string selectedFormatString = "{0:0}";
	
	/// <summary>
	/// How far from the centre to draw the labels (1 = on the circumference). You can use lower or higher values.
	/// </summary>
	public float labelRadius = 0.75f;
	
	/// <summary>
	/// Set to true to draw an outline around the chart.
	/// </summary>
	public bool drawOutline;
	
	/// <summary>
	/// If true show values as percent as percentages.
	/// </summary>
	public bool showValuesAsPercent = false;
	
	/// <summary>
	/// If true show selected values as percentages.
	/// </summary>
	public bool showSelectedValuesAsPercent = false;

	
	private EditorWindow window;
	private Editor editor;
	private float windowHeight;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="PieChart"/> class from an Editor (Inspector).
	/// </summary>
	/// <param name='editor'>
	/// Editor.
	/// </param>
	/// <param name='windowHeight'>
	/// Window height.
	/// </param>
	public PieChart(Editor editor, float windowHeight){
		this.editor = editor;
		this.windowHeight = windowHeight;
		data = new List<float>();
		labels = new List<string>();
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="PieChart"/> class from an EditorWindow.
	/// </summary>
	/// <param name='window'>
	/// Window.
	/// </param>
	/// <param name='windowHeight'>
	/// Window height.
	/// </param>
	public PieChart(EditorWindow window, float windowHeight){
		this.window = window;
		this.windowHeight = windowHeight;
		data = new List<float>();
		labels = new List<string>();
	}
	
	/// <summary>
	/// Draws the chart.
	/// </summary>
	public void DrawChart() {
		Rect rect = GUILayoutUtility.GetRect(Screen.width, windowHeight);
		float sumOfAllData = data.Sum();
		float radius = Mathf.Min (rect.width - xBorder, rect.height - yBorder) / 2.0f;
		Vector2 centre = rect.center;
		Vector2 currentStart = Quaternion.Euler(0,0,-45) * Vector2.up; 
		
		int selected = GetSelectedSegment(centre, sumOfAllData, radius);
		
		// Box/border
		if (boxStyle != null) {
			GUI.Box(new Rect(rect.x + boxStyle.margin.left, rect.y + boxStyle.margin.top, 
				rect.width - (boxStyle.margin.right + boxStyle.margin.left) ,
				rect.height - (boxStyle.margin.top + boxStyle.margin.bottom)),"", boxStyle);
		}
		
		// Draw Background
		for (int j = 0; j < 2; j++){
			Handles.color = new Color(0, 0, 0, 0.15f);
			Handles.DrawSolidDisc(centre + new Vector2(j + 1, j + 2), Vector3.forward, radius + 1);
		}

		// Draw colour
		int i = 0;
		for (int v = 0; v < data.Count; v++) {
			Handles.color = colors[i++];
			if (i > colors.Count - 1) i = 0;
			if (v == selected) Handles.color = selectedColor;
			Handles.DrawSolidArc(centre, Vector3.forward, currentStart, 360 * (data[v]/sumOfAllData), radius);
			currentStart = Quaternion.Euler(0,0,360 * (data[v]/sumOfAllData)) * currentStart;
		}
		
		// Fake AA
		i = 0;
		currentStart = Quaternion.Euler(0,0,-45) * Vector2.up; 
		for (int v = 0; v < data.Count; v++) {
			float offset = 0.25f;
			Handles.color = colors[i++];
			if (v == selected) Handles.color = selectedColor;
			Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.25f);
			if (i > colors.Count - 1) i = 0;
			Handles.DrawSolidArc(centre + (Vector2.right * offset), Vector3.forward, currentStart, 360 * (data[v]/sumOfAllData), radius);
			Handles.DrawSolidArc(centre - (Vector2.right * offset), Vector3.forward, currentStart, 360 * (data[v]/sumOfAllData), radius);
			Handles.DrawSolidArc(centre + (Vector2.up * offset), Vector3.forward, currentStart, 360 * (data[v]/sumOfAllData), radius);
			Handles.DrawSolidArc(centre - (Vector2.up * offset), Vector3.forward, currentStart, 360 * (data[v]/sumOfAllData), radius);
			currentStart = Quaternion.Euler(0,0,360 * (data[v]/sumOfAllData)) * currentStart;
		}
		
		// Draw outline
		if (drawOutline) {
			Handles.color = Color.black;
			currentStart = Quaternion.Euler(0, 0, -45) * Vector2.up; 
			foreach(float v in data){
				Handles.DrawWireArc(centre, Vector3.forward, currentStart, 360 * (v / sumOfAllData), radius);
				Debug.Log (currentStart * radius);
				Handles.DrawAAPolyLine(centre, centre + (currentStart.normalized * radius));
				currentStart = Quaternion.Euler(0,0,360 * (v/sumOfAllData) ) * currentStart;
			}
			Handles.DrawAAPolyLine(centre, centre + (currentStart.normalized * radius));
			Handles.color = Color.white;
		}
		
		// Labels and values
		Vector2 currentPos;
		currentStart = Quaternion.Euler(0,0,-45) * Vector2.up * radius * labelRadius; 
		GUIStyle centeredStyle = new GUIStyle();
		centeredStyle.alignment = TextAnchor.UpperCenter;
		for (int v = 0; v < data.Count && v < labels.Count; v++) {
			currentPos = Quaternion.Euler(0,0,180 * (data[v]/sumOfAllData)) * currentStart;
			Rect labelRect = new Rect(currentPos.x - 50 + rect.x + (rect.width / 2.0f), currentPos.y + rect.y + (rect.height / 2.0f) - 8, 100,50);
			if (labelViewMode == ViewMode.ALWAYS || (labelViewMode == ViewMode.ON_SELECT && selected == v)) {
				GUI.Label(labelRect, labels[v], centeredStyle);
			}
			if (valueViewMode == ViewMode.ALWAYS || (valueViewMode == ViewMode.ON_SELECT && selected == v)) {
				labelRect.y += 16;
				if (selected == v) {
					GUI.Label(labelRect, string.Format(selectedFormatString, (showSelectedValuesAsPercent ? (data[v] / sumOfAllData) * 100.0f: data[v])), centeredStyle);				
				} else {
					GUI.Label(labelRect, string.Format(formatString, (showValuesAsPercent ? (data[v] / sumOfAllData) * 100.0f: data[v])), centeredStyle);				
				}
			}
			currentStart = Quaternion.Euler(0,0,360 * (data[v]/sumOfAllData)) * currentStart;
		}
		
		if (selected != -1) {
			// Listen for click
			if (clickResponder != null &&  Event.current.button == 0 && Event.current.isMouse && Event.current.type == EventType.MouseDown) {
				clickResponder.Click(this, labels[selected], data[selected]);
			}
			if (window != null) window.Repaint();
			if (editor != null) editor.Repaint();
		}
	}
	
	private int GetSelectedSegment(Vector2 centre, float sumOfAllData, float radius) {
	
		if (Vector2.Distance(Event.current.mousePosition, centre) > radius) return -1;
		
		Vector2 currentStart = Quaternion.Euler(0,0,-45) * Vector2.up; 
		Vector2 previousStart = Quaternion.Euler(0,0,-45) * Vector2.up;
		for (int v = 0; v < data.Count; v++) {
			currentStart = Quaternion.Euler(0,0,360 * (data[v]/sumOfAllData)) * currentStart;
			if (Vector2.Dot(previousStart.normalized - (Event.current.mousePosition - centre ).normalized, currentStart.normalized - (Event.current.mousePosition - centre).normalized) < 0.0f) {
				
				return v;	
			}
			previousStart = currentStart;
		}
		return -1;
	}
}
