using UnityEngine;
using System.Collections;

public class TestInspector : MonoBehaviour {
	
	public int random;
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		random = Random.Range (0, 50);
	}
}
