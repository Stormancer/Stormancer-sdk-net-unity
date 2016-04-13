using UnityEngine;
using System.IO;
using System.Collections;
using System;

public class InterpolatorPlugin : Stormancer.SyncBehaviourBase
{
    private bool ReceivedNewPos = false;

    private Vector3 _lastPos = Vector3.zero;
    private Vector3 _lastVect = Vector3.zero;
    private Quaternion _lastRot = Quaternion.identity;

    private Vector3 _targetPos = Vector3.zero;
    private Vector3 _targetVect = Vector3.zero;
    private Quaternion _targetRot = Quaternion.identity;

    private Vector3 P1 = Vector3.zero;
    private Vector3 P2 = Vector3.zero;
    private float _currentSpan;
    private float _targetSpan = 0.2f;
    
    public bool Bezier = true;
    public bool Extrapolate = true;

    public bool ReceivePositionX = true;
    public bool ReceivePositionY = true;
    public bool ReceivePositionZ = true;

    public bool ReceiveRotationX = true;
    public bool ReceiveRotationY = true;
    public bool ReceiveRotationZ = true;

    public override void SendChanges(Stream stream)
    {
        return;
    }

    public override void ApplyChanges(Stream stream)
    {
        using (var reader = new BinaryReader(stream))
        {
            var stamp = reader.ReadInt64();

            float x = 0;
            float y = 0;
            float z = 0;

            if (ReceivePositionX == true)
                x = reader.ReadSingle();
            if (ReceivePositionY == true)
                y = reader.ReadSingle();
            if (ReceivePositionZ == true)
                z = reader.ReadSingle();

            float vx = 0;
            float vy = 0;
            float vz = 0;

            if (ReceivePositionX == true)
                vx = reader.ReadSingle();
            if (ReceivePositionY == true)
                vy = reader.ReadSingle();
            if (ReceivePositionZ == true)
                vz = reader.ReadSingle();

            float rx = 0;
            float ry = 0;
            float rz = 0;

            if (ReceiveRotationX == true)
                rx = reader.ReadSingle();
            if (ReceiveRotationY == true)
                ry = reader.ReadSingle();
            if (ReceiveRotationZ == true)
                rz = reader.ReadSingle();

            var rot = new Quaternion();
            rot = Quaternion.Euler(rx, ry, rz);

            if (LastChanged < stamp)
            {
                LastChanged = stamp;
                    SetNextPos(new Vector3(x, y, z), new Vector3(vx, vy, vz), rot);
            }
        }
    }

    public void SetNextPos(Vector3 pos, Vector3 vect, Quaternion rot)
    {
       // Debug.Log(pos + "  ||  " + vect + "  ||  " + rot);
        _targetSpan = ((float)timeBetweenUpdate) / 1000f;
        ReceivedNewPos = true;

        _lastVect = _targetVect;

        _targetPos = pos;
        _targetVect = vect;
        _targetRot = rot;

        _currentSpan = 0;
    }

    void Update ()
    {
        if (ReceivedNewPos == true)
        {
            _lastPos = transform.position;
            _lastRot = transform.rotation;
            P1 = _lastPos + _lastVect * _targetSpan / 3;
            P2 = _targetPos - _targetVect * _targetSpan / 3;
            ReceivedNewPos = false;
            Debug.DrawLine(_lastPos, P1, Color.gray, 1.0f);
            Debug.DrawLine(P1, P2, Color.gray, 1.0f);
            Debug.DrawLine(P2, _targetPos, Color.gray, 1.0f);
        }


        _currentSpan += Time.deltaTime;
        if (_currentSpan < _targetSpan)
        {
            float perc = _currentSpan / _targetSpan;
            if (Bezier == false)
                transform.position = Vector3.Lerp(_lastPos, _targetPos, perc);
            else
            {
                var l1 = Vector3.Lerp(_lastPos, P1 , perc);
                var l2 = Vector3.Lerp(P1, P2, perc);
                var l3 = Vector3.Lerp(P2, _targetPos, perc);

                Debug.DrawLine(l1, l2, Color.green, Time.deltaTime);
                Debug.DrawLine(l2, l3, Color.green, Time.deltaTime);

                var f1 = Vector3.Lerp(l1, l2, perc);
                var f2 = Vector3.Lerp(l2, l3, perc);

                Debug.DrawLine(f1, f2, Color.blue, Time.deltaTime);

                transform.position = Vector3.Lerp(f1, f2, perc);
            }
            transform.rotation = Quaternion.Lerp(_lastRot, _targetRot, perc);
        }
        else if (Extrapolate == true)
        {
            _lastPos = transform.position;
            _lastVect = _targetVect;
            _lastRot = transform.rotation;

            _targetPos = _targetPos + (_targetVect * _targetSpan);

            P1 = _lastPos + _lastVect * _targetSpan / 3;
            P2 = _targetPos - _targetVect * _targetSpan / 3;
            _currentSpan = 0;
        }
	}
}
