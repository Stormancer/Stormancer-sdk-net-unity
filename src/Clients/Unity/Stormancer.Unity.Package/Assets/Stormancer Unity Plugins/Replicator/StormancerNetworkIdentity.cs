using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Stormancer
{
    public class StormancerNetworkIdentity : MonoBehaviour
    {
        public bool IsMaster = false;
        public uint Id;
        public int PrefabId;
        public long MasterId;

        public List<SyncBehaviourBase> SynchBehaviours { get; set; }
        
        void Awake()
        {
            SynchBehaviours = new List<SyncBehaviourBase>(this.GetComponents<SyncBehaviourBase>());
            Debug.Log("created network identity with " + SynchBehaviours.Count + " behaviours");
        } 
    }
}