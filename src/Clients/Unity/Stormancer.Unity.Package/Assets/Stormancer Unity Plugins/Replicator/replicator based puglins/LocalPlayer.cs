using UnityEngine;
using System.Collections;
using System.IO;
using System;

public class LocalPlayer : Stormancer.SyncBehaviourBase
{
    public Stormancer.RemoteScene Scene;
    private Stormancer.IClock Clock;
    private Rigidbody PlayerRigidbody;

    public bool SendPositionX = true;
    public bool SendPositionY = true;
    public bool SendPositionZ = true;

    public bool SendRotationX = true;
    public bool SendRotationY = true;
    public bool SendRotationZ = true;

    public override void SendChanges(Stream stream)
    {
        if (Clock == null)
        {
            Clock = Scene.Scene.DependencyResolver.Resolve<Stormancer.IClock>();
        }
        if (PlayerRigidbody == null)
        {
            PlayerRigidbody = this.GetComponent<Rigidbody>();
        }
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8))
        {

            writer.Write(Clock.Clock);
            if (SendPositionX)
            {
                writer.Write(this.transform.position.x);
            }
            if (SendPositionY)
            {
                writer.Write(this.transform.position.y);
            }
            if (SendPositionZ)
            {
                writer.Write(this.transform.position.z);
            }
            if (SendPositionX)
            {
                writer.Write(PlayerRigidbody.velocity.x);               
            }
            if (SendPositionY)
            {
                writer.Write(PlayerRigidbody.velocity.y);
            }
            if (SendPositionZ)
            {
                writer.Write(PlayerRigidbody.velocity.z);
            }

            

            var rot = this.transform.rotation.eulerAngles;

            if (SendRotationX)
            {
                writer.Write(rot.x);
            }
            if (SendRotationY)
            {
                writer.Write(rot.y);
            }
            if (SendRotationZ)
            {
                writer.Write(rot.z);
            }
        }
    }

    public override void ApplyChanges(Stream stream)
    {
        using (var reader = new BinaryReader(stream))
        {
            var stamp = reader.ReadInt64();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();

            var vx = reader.ReadSingle();
            var vy = reader.ReadSingle();
            var vz = reader.ReadSingle();

            var rx = reader.ReadSingle();
            var ry = reader.ReadSingle();
            var rz = reader.ReadSingle();
            var rw = reader.ReadSingle();

            if (_lastChanged < stamp)
            {
                _lastChanged = stamp;
                Stormancer.MainThread.Post(() =>
                {
                    this.transform.position = new Vector3(x, y, z);
                    PlayerRigidbody.velocity = new Vector3(vx, vy, vz);
                    this.transform.rotation = new Quaternion(rx, ry, rz, rw);
                });
            }
        }
    }
}
