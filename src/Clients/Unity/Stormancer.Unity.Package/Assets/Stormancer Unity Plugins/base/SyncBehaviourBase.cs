using UnityEngine;
using System.Collections;
using Stormancer.Core;
using System.IO;

namespace Stormancer
{
    public abstract class SyncBehaviourBase : MonoBehaviour
    {
        //Unity
        public long timeBetweenUpdate = 200;

        //Not Unity
        public long LastSend { get; set; }
        public long LastChanged { get; set; }

        public abstract void SendChanges(Stream stream);
        public abstract void ApplyChanges(Stream stream);

        public PacketReliability Reliability = PacketReliability.UNRELIABLE_SEQUENCED;

        public bool synch { get; private set; }

        public void SynchImmediate()
        {
            synch = true;
        }
    }
}
