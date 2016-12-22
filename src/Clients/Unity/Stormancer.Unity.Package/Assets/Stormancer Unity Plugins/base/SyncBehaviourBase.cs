using UnityEngine;
using System.Collections;
using Stormancer.Core;
using System.IO;

namespace Stormancer
{
    [RequireComponent(typeof(StormancerNetworkIdentity))]
    public abstract class SyncBehaviourBase : MonoBehaviour
    {
        //Unity
        public long timeBetweenUpdate = 150;

        //Not Unity
        public long LastSend { get; set; }
        protected long _lastChanged;

        public abstract void SendChanges(Stream stream);
        public abstract void ApplyChanges(Stream stream);

        public long TimeStamp
        {
            get
            {
                return NetworkIdentity.TimeStamp;
            }
        }

        protected StormancerNetworkIdentity NetworkIdentity { get; private set; }

        public virtual void Awake()
        {
            NetworkIdentity = GetComponent<StormancerNetworkIdentity>();
        }

        public PacketReliability Reliability = PacketReliability.UNRELIABLE_SEQUENCED;

    }
}
