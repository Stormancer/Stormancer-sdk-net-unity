using UnityEngine;
using System.Collections;
using System;

public class Interpolation : MonoBehaviour {

    public float UpdatePeriod;
	private float UpdateFrequency
	{
		get
		{
			return 1f / this.UpdatePeriod; 
		}
	}


	private bool _initialized;
	private float _lastUpdateTime;
	private Vector3 _lastPosition;
	private Vector3 _targetPosition;
	private Quaternion _lastRotation;
	private Quaternion _targetRotation;
	private Vector3 _speed;
	private Quaternion _angularSpeed;

	public void Move(Vector3 position, Quaternion rotation)
	{
		this._lastUpdateTime = Time.time;
		if (this._initialized) {			
			this._lastPosition = this.transform.position;
			this._lastRotation = this.transform.rotation;
				}
		else {
			this._lastPosition = position;
			this._lastRotation = rotation;
			this._initialized = true;
				}
		this._targetPosition = position;
		this._targetRotation = rotation;

		this._speed = this._targetPosition - this._lastPosition;
		var frequency = this.UpdateFrequency;
		this._speed.Scale (new Vector3 (frequency,frequency,frequency));
	}

	// Use this for initialization
	void Start () {	
	}
	
	// Update is called once per frame
	void Update () {
		float dt = (Time.time - this._lastUpdateTime) / this.UpdatePeriod;
		var newposition = Vector3.Lerp (this._lastPosition, this._targetPosition, dt);
		this.transform.position = newposition;

		var newRotation = Quaternion.Slerp (this._lastRotation, this._targetRotation, dt);
		this.transform.rotation = newRotation;
	}
}
