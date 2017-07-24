using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class TestScript : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start () {
        Debug.Log("Get public scene.");
        var t = ClientProvider.GetPublicScene("authenticator", "");
        yield return t.AsCouroutine();
        var scene = t.Result;
        Debug.Log("Connect to scene.");
        var connectionTask = scene.Connect();
        yield return connectionTask.AsCouroutine();
        connectionTask.Wait();
        Debug.Log("Connected to scene.");
    }

    // Update is called once per frame
    //void Update () {

    //}
}
