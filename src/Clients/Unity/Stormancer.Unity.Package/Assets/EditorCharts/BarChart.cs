using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Draw bar and stacked bar charts in 2D or 3D.
/// </summary>
public class BarChart {
	
	/// <summary>
	/// The data driving the chart.
	/// </summary>
   	public List <float[]> data;
	
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
	public List<Color> colors = new List<Color>(){Colors.PastelGreen, Colors.PastelYellow, Colors.PastelBlue};
	
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
	
	private EditorWindow window;
	private Editor editor;
	private float windowHeight;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="BarChart"/> class from an Editor (Inspector).
	/// </summary>
	/// <param name='editor'>
	/// Editor.
	/// </param>
	/// <param name='windowHeight'>
	/// Window height.
	/// </param>
	public BarChart(Editor editor, float windowHeight){
		this.editor = editor;
		this.windowHeight = windowHeight;
		data = new List<float[]>();
		labels = new List<string>();
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="BarChart"/> class from an EditorWindow.
	/// </summary>
	/// <param name='window'>
	/// Window.
	/// </param>
	/// <param name='windowHeight'>
	/// Window height.
	/// </param>
	public BarChart(EditorWindow window, float windowHeight){
		this.window = window;
		this.windowHeight = windowHeight;
		data = new List<float[]>();
		labels = new List<string>();
	}
	
	/// <summary>
	/// Draws the chart.
	/// </summary>
	public void DrawChart() {
		
		Rect rect = GUILayoutUtility.GetRect(Screen.width, windowHeight);
		float barTop = rect.y + yBorder;
		float barWidth = (float) (Screen.width - (xBorder * 2))/ ((data.Count * 1.5f) + 1);
		float barFloor = rect.y + rect.height - yBorder;
 		float dataMax = 0.0f;
        for (int i = 0; i < data.Count; i++) {
            if (data[i].Sum() > dataMax) {
                dataMax = data[i].Sum();        
            }
        }
		Rect currentRect;
		
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
				GUI.Label(new Rect(0, barTop + (lineSpacing * i) - 8, xBorder - 2, 50), string.Format(axisFormatString, (dataMax * (1 - ((lineSpacing * i) / (barFloor - barTop))))) , labelTextStyle);
			}
			Handles.color = Color.white;
		}
		
		// Draw bars
		GUIStyle centeredStyle = new GUIStyle();
		centeredStyle.alignment = TextAnchor.UpperCenter;
		centeredStyle.normal.textColor = fontColor;
		
        for (int i = 0; i < data.Count; i++) {
            float currentHeight = 0.0f;
            int c = 0;
            for (int j = 0; j < data[i].Length; j++) {
                float height = (barFloor - barTop) * (data[i][j] / dataMax);
                currentRect =  new Rect(((i * 1.5f) + 0.5f) * barWidth + xBorder , barFloor - currentHeight - height, barWidth, height);
                if (height > 0) {
                    if (currentRect.Contains(Event.current.mousePosition)) {
                        //GUI.color = selectedColor;
                        Draw3DBar(new Vector3(((i * 1.5f) + 0.5f) * barWidth + xBorder, barFloor - currentHeight, 0.0f), barWidth, height, depth, selectedColor, Color.black);
                        // Draw value
                        Rect labelRect = currentRect;
                        if (valueViewMode == ViewMode.ON_SELECT || valueViewMode == ViewMode.ALWAYS) {
                            if (labelRect.height < 16) { labelRect.height = 16; labelRect.y -= 16 + depth; }
                            GUI.Label(labelRect, string.Format(formatString, data[i][j]), centeredStyle);
                        }
                        // Draw label
                        if (j == 0) {
                            if (labelViewMode == ViewMode.ON_SELECT && i < labels.Count) {
                                labelRect.height = 16; labelRect.y -= 16 + depth;
                                GUI.Label(labelRect, labels[i], centeredStyle);    
                            }
                        }
                        // Listen for click
                        if (clickResponder != null &&  Event.current.button == 0 && Event.current.isMouse && Event.current.type == EventType.MouseDown) {
                            clickResponder.Click(this, labels[i], data[i][j]);
                        }
                        if (window != null) window.Repaint();
						if (editor != null) editor.Repaint();
                    } else {
                        Draw3DBar(new Vector3(((i * 1.5f) + 0.5f) * barWidth + xBorder, barFloor - currentHeight, 0.0f), barWidth, height, depth, colors[c++], Color.black);
                        if (valueViewMode == ViewMode.ALWAYS) {
                            if (currentRect.height < 16) { currentRect.height = 16; currentRect.y -= 16 + depth; }
                            GUI.Label(currentRect, string.Format(formatString, data[i][j]), centeredStyle);    
                        }
                    }
                    
                }
                if (labelViewMode == ViewMode.ALWAYS && i < labels.Count && j == 0) {
                    Rect labelRect = new Rect(currentRect.x, currentRect.y + currentRect.height + 1, currentRect.width, 16);
                    GUI.Label(labelRect, labels[i], centeredStyle);    
                }
                currentHeight += height;
                if (c > colors.Count - 1) c = 0;
            }
            
            GUI.color = Color.white;   
        }
		
		// Draw Axis
		Handles.color = axisColor;
    	Handles.DrawLine(new Vector2(xBorder, barTop), new Vector2(xBorder, barFloor));
		Handles.DrawLine(new Vector2(xBorder, barFloor), new Vector2(Screen.width - xBorder, barFloor));
		Handles.color = Color.white;
	}
	
	private void Draw3DBar(Vector3 offset, float width, float height, float depth, Color color, Color outlineColor) {
		List <Vector3> verts = new List<Vector3>();
		verts.Add(new Vector3(offset.x, offset.y,0));
		verts.Add(new Vector3(offset.x + width, offset.y,0));
		verts.Add(new Vector3(offset.x + width, offset.y - height,0));
		verts.Add(new Vector3(offset.x, offset.y - height,0));
		Handles.DrawSolidRectangleWithOutline(verts.ToArray(), color, outlineColor);
		if (depth > 0) {
			verts.Clear();
			verts.Add(new Vector3(offset.x + width, offset.y - height,0));
			verts.Add(new Vector3(offset.x, offset.y - height,0));
			verts.Add(new Vector3(offset.x + depth, offset.y - height - depth,0));
			verts.Add(new Vector3(offset.x + width + depth, offset.y - height - depth,0));
			Handles.DrawSolidRectangleWithOutline(verts.ToArray(), new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f), outlineColor);
			
			verts.Clear();
			verts.Add(new Vector3(offset.x + width, offset.y,0));
			verts.Add(new Vector3(offset.x + width, offset.y - height,0));
			verts.Add(new Vector3(offset.x + width + depth, offset.y - height - depth,0));
			verts.Add(new Vector3(offset.x + width + depth, offset.y - depth,0));
			Handles.DrawSolidRectangleWithOutline(verts.ToArray(), new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f), outlineColor);
		}
	}
	
	/// <summary>
	/// Convenience method to set the data to a list of floats.
	/// </summary>
	/// <param name='data'>
	/// Chart data as a list of floats.
	/// </param>
	public void SetData(List<float> data) {
		this.data = new List<float[]>();
		foreach (float d in data){
			this.data.Add(new float[]{d});
		}
	}
}