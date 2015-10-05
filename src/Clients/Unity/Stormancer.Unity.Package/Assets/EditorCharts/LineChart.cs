using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LineChart {
	
	/// <summary>
	/// The data driving the chart.
	/// </summary>
	public List <float>[] data;
	
	/// <summary>
	/// The labels of the data items.
	/// </summary>
	public List <string> dataLabels;
	
	/// <summary>
	/// The labels on the axis.
	/// </summary>
	public List <string> axisLabels;
	
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
	public List<Color> colors = new List<Color>{Color.magenta, Color.cyan * 2.0f, Color.green};	
	
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
	/// Size of the pips that occur at each data point. Use 1 or smaller to draw straight lines.
	/// </summary>
	public float pipRadius = 3.0f;
	
	/// <summary>
	/// If true draw small lines along the x axis at each data point.
	/// </summary>
	public bool drawTicks = true;
	
	private float barFloor ;
	private float barTop;
	private	float lineWidth;
	private float dataMax;
	
	private EditorWindow window;
	private Editor editor;
	private float windowHeight;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="LineChart"/> class from an Editor (Inspector).
	/// </summary>
	/// <param name='editor'>
	/// Editor.
	/// </param>
	/// <param name='windowHeight'>
	/// Window height.
	/// </param>
	public LineChart(Editor editor, float windowHeight){
		this.editor = editor;
		this.windowHeight = windowHeight;
		dataLabels = new List<string>();
		axisLabels = new List<string>();
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="LineChart"/> class from an Editor Window.
	/// </summary>
	/// <param name='window'>
	/// Window.
	/// </param>
	/// <param name='windowHeight'>
	/// Window height.
	/// </param>
	public LineChart(EditorWindow window, float windowHeight){
		this.window = window;
		this.windowHeight = windowHeight;
		dataLabels = new List<string>();
		axisLabels = new List<string>();
	}
	
	/// <summary>
	/// Draws the chart.
	/// </summary>
	public void DrawChart() {	
		
		if (data.Length > 0) {
			
			Rect rect = GUILayoutUtility.GetRect(Screen.width, windowHeight);
			barTop = rect.y + yBorder;
			lineWidth = (float) (Screen.width - (xBorder * 2)) / data[0].Count;
			barFloor = rect.y + rect.height - yBorder;
			dataMax = 0.0f;
			foreach (List<float> row in data) {
				if (row != null && row.Count > 0) {
					if (row.Max() > dataMax) {
						dataMax = row.Max();
					}
				}
			}
			
			// Box/border
			if (boxStyle != null) {
				GUI.Box(new Rect(rect.x + boxStyle.margin.left, rect.y + boxStyle.margin.top, 
					rect.width - (boxStyle.margin.right + boxStyle.margin.left) ,
					rect.height - (boxStyle.margin.top + boxStyle.margin.bottom)),"", boxStyle);
			}
			
			
			// Clean up variables
			if (dataMax % axisRounding != 0){
				dataMax = dataMax + axisRounding - (dataMax % axisRounding);
			}
			
			// Text to Left
			GUIStyle labelTextStyle = new GUIStyle();
	    	labelTextStyle.alignment = TextAnchor.UpperRight;
			labelTextStyle.normal.textColor = fontColor;
			
			// Draw grid lines
			if (gridLines > 0) { 
				Handles.color = Color.grey;
				float lineSpacing = (barFloor - barTop) / (gridLines + 1);
				for (int i = 0; i <= gridLines; i++) {
					if (i > 0) Handles.DrawLine(new Vector2(xBorder, barTop + (lineSpacing * i)), new Vector2(Screen.width - xBorder, barTop + (lineSpacing * i)));	 
					if ((dataMax * (1 - ((lineSpacing * i) / (barFloor - barTop)))) > 0)
					GUI.Label(new Rect(0, barTop + (lineSpacing * i) - 8, xBorder - 2, 50), string.Format(axisFormatString, (dataMax * (1 - ((lineSpacing * i) / (barFloor - barTop))))) , labelTextStyle);
				}
				Handles.color = Color.white;
			}
			
			int c = 0;
			for (int i = 0; i < data.Length; i++) {
				if (data[i] != null) {
					DrawLine (data[i], colors[c++], i < dataLabels.Count ? dataLabels[i] : "");
					if (c > colors.Count - 1) c = 0;
				}
			}
			
			// Draw Axis
			Handles.color = axisColor;
	    	Handles.DrawLine(new Vector2(xBorder, barTop), new Vector2(xBorder, barFloor));
			Handles.DrawLine(new Vector2(xBorder, barFloor), new Vector2(Screen.width - xBorder, barFloor));
			
			GUIStyle centeredStyle = new GUIStyle();
			centeredStyle.alignment = TextAnchor.UpperCenter;
			centeredStyle.normal.textColor = fontColor;
			
			// Draw ticks and labels
			for (int i = 0; i < data[0].Count; i++) {
				if (i > 0 && drawTicks) Handles.DrawLine(new Vector2(xBorder + (lineWidth * i), barFloor - 3), new Vector2(xBorder + (lineWidth * i), barFloor + 3));
				if (i < axisLabels.Count) {
					Rect labelRect = new Rect(xBorder + (lineWidth * i) - lineWidth / 2.0f, barFloor + 5, lineWidth, 16);			
					GUI.Label(labelRect, axisLabels[i], centeredStyle);				
				}
			}
			
			Handles.color = Color.white;
		}
	}
	
	private void DrawLine(List<float> data, Color color, string label) {
		Vector2 previousLine = Vector2.zero;
		Vector2 newLine;
		Handles.color = color;
		
		for (int i = 0; i < data.Count; i++) {
			float lineTop = barFloor - ((barFloor - barTop) * (data[i] / dataMax));
			newLine = new Vector2(xBorder + (lineWidth * i), lineTop);
			if (i > 0) {	
				Handles.DrawAAPolyLine(previousLine, newLine);
			}
			previousLine = newLine;
			Rect selectRect = new Rect((previousLine - (Vector2.up * 0.5f)).x - pipRadius * 3, (previousLine - (Vector2.up * 0.5f)).y - pipRadius * 3, pipRadius * 6, pipRadius * 6);
			if (selectRect.Contains(Event.current.mousePosition)) {
				GUIStyle centeredStyle = new GUIStyle();
				centeredStyle.alignment = TextAnchor.UpperCenter;
				centeredStyle.normal.textColor = fontColor;
				Handles.DrawSolidDisc(previousLine - (Vector2.up * 0.5f), Vector3.forward, pipRadius * 2);
				if (valueViewMode == ViewMode.ON_SELECT) {
					selectRect.y -= 16; selectRect.width += 50; selectRect.x -= 25;
					GUI.Label(selectRect, string.Format(formatString, data[i]), centeredStyle);				
				}
				// Listen for click
				if (clickResponder != null &&  Event.current.button == 0 && Event.current.isMouse && Event.current.type == EventType.MouseDown) {
					clickResponder.Click(this, dataLabels[i], data[i]);
				}
				if (window != null) window.Repaint();
				if (editor != null) editor.Repaint();
			} else {
				Handles.DrawSolidDisc(previousLine - (Vector2.up * 0.5f), Vector3.forward, pipRadius);
			}
		}	
		if (label != null) {
			GUIStyle colorStyle = new GUIStyle();
			colorStyle.normal.textColor = color;
			Rect labelRect = new Rect(previousLine.x + 8, previousLine.y - 8, 100, 16);			
			GUI.Label(labelRect, label, colorStyle);				
		}
	}
	
}