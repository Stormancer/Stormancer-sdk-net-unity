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
        public long TimeOffset = 300;
        
        private bool _started;
        private bool _registered;
        private IClock _synchClock;

        public ReplicatorBehaviour replicator;

        public bool Activated { get; private set; }

        public void ActivateSlave()
        {
            this.gameObject.SetActive(true);
            Activated = true;
        }
        
        public ReplicatorBehaviour Replicator
        {
            get
            {
                return replicator ?? GameObject.FindObjectOfType<ReplicatorBehaviour>();
            }
            set
            {
                replicator = value;
            }
        }

        public long TimeStamp
        {
            get
            {
                if (IsMaster)
                {
                    return _synchClock.Clock;
                }
                else
                {
                    return _synchClock.Clock - TimeOffset;
                }
            }
        }

        public List<SyncBehaviourBase> SynchBehaviours { get; set; }


        void Awake()
        {
            SynchBehaviours = new List<SyncBehaviourBase>(this.GetComponents<SyncBehaviourBase>());
            Debug.Log("created network identity with " + SynchBehaviours.Count + " behaviours");
            
        }

        public void OnEnable()
        {
            if (_started)
            {
                RegisterToReplicator();
            }

        }

        private void RegisterToReplicator()
        {
            if (!IsMaster)
            {
                return;
            }

            _registered = true;
            if (Replicator != null)
            {
                Replicator.AddObjectToSynch(this);
            }
        }

        public void OnDisable()
        {
            if (_registered)
            {
                UnregisterFromReplicator();
            }
        }

        private void UnregisterFromReplicator()
        {
            if (!IsMaster)
            {
                return;
            }

            _registered = false;

            if (Replicator != null)
            {
                Replicator.RemoveSynchObject(this);
            }
        }

        public void Start()
        {
            _started = true;
            RegisterToReplicator();
        }

        public void OnDestroy()
        {
            if(_registered)
            {
                UnregisterFromReplicator();
            }
        }

        public void SetSyncClock(IClock clock)
        {
            _synchClock = clock;
        }
    }
}