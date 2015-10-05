using UnityEngine;
using System.Collections;

/// <summary>
/// Implement this interface on the class you want to listen to chart click events.
/// </summary>
public interface ClickResponder {
	
	/// <summary>
	/// The chart got clicked!
	/// </summary>
	/// <param name='chart'>
	/// Chart that was clicked.
	/// </param>
	/// <param name='label'>
	/// Label of the area that was clicked.
	/// </param>
	/// <param name='value'>
	/// Value of the area that was clicked.
	/// </param>
	void Click(object chart, string label, float value);
	
}