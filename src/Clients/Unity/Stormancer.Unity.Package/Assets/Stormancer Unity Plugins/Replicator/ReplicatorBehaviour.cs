using UnityEngine;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Stormancer;
using Stormancer.Core;
using Stormancer.Diagnostics;
using System.Threading.Tasks;
using Stormancer.Plugins;
using System.IO;
using System.Linq;
using System;
using System.Collections;

namespace Stormancer
{
    public struct ReplicatorDTO
    {
        public uint Id;
        public int PrefabId;
        public long ClientId;
    }

    public class ReplicatorBehaviour : RemoteLogicBase
    {
        public bool UseDebugGhost = false;
        public float Persistence = 0;

        public List<StormancerNetworkIdentity> Prefabs;
        private readonly List<StormancerNetworkIdentity> _localObjectToSync = new List<StormancerNetworkIdentity>();
        private readonly ConcurrentDictionary<uint, StormancerNetworkIdentity> _slaveObjects = new ConcurrentDictionary<uint, StormancerNetworkIdentity>();
        private readonly ConcurrentDictionary<uint, StormancerNetworkIdentity> _mastersObjects = new ConcurrentDictionary<uint, StormancerNetworkIdentity>();

        private readonly ConcurrentDictionary<uint, int> _unknownObjects = new ConcurrentDictionary<uint, int>();

        private IClock _clock;


        public override void Init(Scene s)
        {
            if (s != null)
            {
                Debug.Log("replicator initializing");
                _clock = s.DependencyResolver.Resolve<IClock>();
                s.AddProcedure("RequestObjects", OnRequestObjects);
                s.AddRoute("PlayerDisconnected", OnPlayerDisconnect);
                s.AddRoute("CreateObject", OnCreateObject);
                s.AddRoute("DestroyObject", OnDestroyObject);
                s.AddRoute("ForceUpdate", OnForceUpdate);
                s.AddRoute("UpdateObject", OnUpdateObject);
                s.AddRoute("UntargettedEvent", OnUntargettedEvent);
                s.AddRoute("TargettedEvent", OnTargettedEvent);
            }
        }
        public override void OnConnected()
        {
            Debug.Log("replicator connected");
            foreach (StormancerNetworkIdentity ni in _localObjectToSync)
            {
                AddObjectToSynch(ni);
            }
        }

        public void AddObjectToSynch(StormancerNetworkIdentity ni)
        {
            if (!_localObjectToSync.Contains(ni))
            {
                _localObjectToSync.Add(ni);
            }

            StormancerNetworkIdentity existingNetworkIdentity;
            if (RemoteScene.Scene == null || !RemoteScene.Scene.Connected || (_mastersObjects.TryGetValue(ni.Id, out existingNetworkIdentity) && ni == existingNetworkIdentity))
            {
                // scene is not connected yet or object is already registered
                return;
            }

            ni.SetSyncClock(_clock);

            Debug.Log("sending registration request");
            var dto = new ReplicatorDTO();
            dto.PrefabId = ni.PrefabId;
            dto.ClientId = ClientProvider.GetClientId();
            RemoteScene.Scene.RpcTask<ReplicatorDTO, ReplicatorDTO>("RegisterObject", dto).ContinueWith(response =>
            {
                MainThread.Post(() =>
                    {
                        Debug.Log("received registration");
                        dto = response.Result;
                        ni.Id = dto.Id;
                        ni.MasterId = ClientProvider.GetClientId();
                        _mastersObjects.TryAdd(dto.Id, ni);
                        if (!UseDebugGhost && _slaveObjects.ContainsKey(dto.Id))
                        {
                            StormancerNetworkIdentity trash;
                            _slaveObjects.TryRemove(dto.Id, out trash);

                            Destroy(trash.gameObject);
                        }
                        if (UseDebugGhost && !_slaveObjects.ContainsKey(dto.Id))
                        {
                            var synchedGO = Instantiate(Prefabs[dto.PrefabId]);

                            var collider = synchedGO.GetComponent<Collider>();
                            if (collider != null)
                            {
                                collider.enabled = false;
                            }

                            synchedGO.Id = dto.Id;
                            synchedGO.PrefabId = dto.PrefabId;
                            synchedGO.MasterId = dto.ClientId;
                            _slaveObjects.TryAdd(dto.Id, synchedGO);
                            synchedGO.Replicator = this;
                            synchedGO.SetSyncClock(_clock);
                            synchedGO.gameObject.SetActive(false);
                        }
                    });
            });
        }

        public Task OnRequestObjects(RequestContext<IScenePeer> ctx)
        {
            List<ReplicatorDTO> dtos = new List<ReplicatorDTO>();

            foreach (StormancerNetworkIdentity ni in _localObjectToSync)
            {
                ReplicatorDTO dto = new ReplicatorDTO();

                dto.Id = ni.Id;
                dto.PrefabId = ni.PrefabId;
                dto.ClientId = ClientProvider.GetClientId();

                dtos.Add(dto);
            }
            ctx.SendValue<List<ReplicatorDTO>>(dtos);
            Debug.Log("receiving object request for new player. Sent " + dtos.Count + " objects.");
            return TaskHelper.FromResult(true);
        }

        public void OnPlayerDisconnect(Packet<IScenePeer> packet)
        {
            var clientId = packet.ReadObject<long>();
            int i = 0;

            foreach (StormancerNetworkIdentity ni in _slaveObjects.Values)
            {
                if (ni.MasterId == clientId)
                {
                    i++;
                    StormancerNetworkIdentity trash;
                    _slaveObjects.TryRemove(ni.Id, out trash);
                    MainThread.Post(() =>
                    {
                        Destroy(trash.gameObject);
                    });
                }
            }
            Debug.Log("a player disconnected. Removed " + i + " objects");
        }

        public void RemoveSynchObject(StormancerNetworkIdentity ni)
        {
            _localObjectToSync.Remove(ni);

            Debug.Log("removing object");
            var dto = new ReplicatorDTO();
            dto.Id = ni.Id;
            dto.ClientId = ClientProvider.GetClientId();
            if (_mastersObjects.TryRemove(ni.Id, out ni))
            {
                _localObjectToSync.Remove(ni);
                RemoteScene.Scene.Send<ReplicatorDTO>("RemoveObject", dto);
            }

        }

        private void OnCreateObject(Packet<IScenePeer> packet)
        {
            var dto = packet.ReadObject<ReplicatorDTO>();

            int _;
            _unknownObjects.TryRemove(dto.Id, out _);

            Debug.Log("creating object");
            if (dto.PrefabId < Prefabs.Count && !_slaveObjects.ContainsKey(dto.Id) && !_mastersObjects.ContainsKey(dto.Id))
            {
                MainThread.Post(() =>
                {
                    var synchedGO = Instantiate(Prefabs[dto.PrefabId]);
                    synchedGO.Id = dto.Id;
                    synchedGO.PrefabId = dto.PrefabId;
                    synchedGO.MasterId = dto.ClientId;
                    _slaveObjects.TryAdd(dto.Id, synchedGO);
                    synchedGO.Replicator = this;
                    synchedGO.SetSyncClock(_clock);
                    synchedGO.gameObject.SetActive(false);
                });
            }
        }

        private void OnDestroyObject(Packet<IScenePeer> packet)
        {
            var dto = packet.ReadObject<ReplicatorDTO>();
            StormancerNetworkIdentity destroyedGO;
            Debug.Log("destroying object");

            if (_slaveObjects.TryGetValue(dto.Id, out destroyedGO))
            {
                MainThread.Post(() =>
                {
                    if (Persistence > 0)
                    {
                        StartCoroutine(DestroyAfterPersistance(destroyedGO));
                    }
                    else
                    {
                        _slaveObjects.TryRemove(dto.Id, out destroyedGO);
                        Destroy(destroyedGO.gameObject);
                    }
                });
            }
        }

        private IEnumerator DestroyAfterPersistance(StormancerNetworkIdentity destroyedGO)
        {
            yield return new WaitForSeconds(Persistence);
            StormancerNetworkIdentity _;
            if (_slaveObjects.TryRemove(destroyedGO.Id, out _))
            {
                Destroy(destroyedGO.gameObject);
            }
        }

        private void OnForceUpdate(Packet<IScenePeer> packet)
        {
            var reader = new BinaryReader(packet.Stream);
            var id = reader.ReadUInt32();
            var synchedBehaviourId = reader.ReadByte();
            StormancerNetworkIdentity synchedObject;

            if (_mastersObjects.TryGetValue(id, out synchedObject) && synchedBehaviourId < synchedObject.SynchBehaviours.Count)
            {
                MainThread.Post(() =>
                {
                    using (reader)
                    {
                        synchedObject.SynchBehaviours[synchedBehaviourId].ApplyChanges(packet.Stream);
                    }
                });
            }

        }

        private void OnUpdateObject(Packet<IScenePeer> packet)
        {
            var reader = new BinaryReader(packet.Stream);

            var id = reader.ReadUInt32();
            var synchedBehaviourId = reader.ReadByte();
            StormancerNetworkIdentity synchedObject;

            if (_slaveObjects.TryGetValue(id, out synchedObject) && synchedBehaviourId < synchedObject.SynchBehaviours.Count)
            {
                MainThread.Post(() =>
                {
                    using (reader)
                    {
                        synchedObject.SynchBehaviours[synchedBehaviourId].ApplyChanges(packet.Stream);
                        if (!synchedObject.Activated)
                        {
                            synchedObject.ActivateSlave();
                        }
                    }
                });
            }
            else
            {
                if (!_slaveObjects.ContainsKey(id) && !_mastersObjects.ContainsKey(id))
                {
                    if (_unknownObjects.AddOrUpdate(id, 1, (_, oldvalue) => oldvalue + 1) > 10)
                    {
                        int __;
                        _unknownObjects.TryRemove(id, out __);
                        RemoteScene.Scene.Send("QueryObject", id);
                    }
                }
            }
        }

        void Update()
        {
            if (RemoteScene != null && RemoteScene.Scene != null && RemoteScene.Scene.Connected && _mastersObjects.Count > 0)
            {
                foreach (KeyValuePair<uint, StormancerNetworkIdentity> kvp in _mastersObjects)
                {
                    byte i = 0;
                    foreach (SyncBehaviourBase sb in kvp.Value.SynchBehaviours)
                    {
                        if (sb.LastSend + sb.timeBetweenUpdate < _clock.Clock)
                        {
                            sb.LastSend = _clock.Clock;
                            RemoteScene.Scene.SendPacket("UpdateSynchedObject", stream =>
                            {
                                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8))
                                {
                                    writer.Write((byte)sb.Reliability);
                                    writer.Write(kvp.Key);
                                    writer.Write(i);
                                    sb.SendChanges(stream);
                                }
                            }, PacketPriority.MEDIUM_PRIORITY, sb.Reliability);
                        }
                        i++;
                    }
                }
            }
        }


        public bool TryGetSynchedObject(uint id, out StormancerNetworkIdentity identity)
        {
            return _slaveObjects.TryGetValue(id, out identity) || _mastersObjects.TryGetValue(id, out identity);
        }


        public StormancerNetworkIdentity GetSynchedObject(uint id)
        {
            StormancerNetworkIdentity result;

            if (TryGetSynchedObject(id, out result))
            {
                return result;
            }
            else
            {
                Debug.LogError("No object with key " + id + " is present in the synched objects");
                throw new KeyNotFoundException("No object with key " + id + " is present in the synched objects");
            }
        }

        public void SendEvent(StormancerNetworkIdentity eventSource, byte eventId, PacketReliability reliability, Action<Stream> writer)
        {
            SendEventImpl(eventSource, null, eventId, reliability, writer, false);
        }

        public void SendEvent(StormancerNetworkIdentity eventSource, StormancerNetworkIdentity eventTarget, byte eventId, PacketReliability reliability, Action<Stream> writer)
        {
            SendEventImpl(eventSource, eventTarget, eventId, reliability, writer, true);
        }

        private void SendEventImpl(StormancerNetworkIdentity eventSource, StormancerNetworkIdentity eventTarget, byte eventId, PacketReliability reliability, Action<Stream> writer, bool targetted)
        {
            if(RemoteScene == null || RemoteScene.Scene == null || !RemoteScene.Scene.Connected)
            {
                return;
            }

            RemoteScene.Scene.SendPacket(targetted ? "TargettedEvent" : "UntargettedEvent", s =>
              {
                  using (var binaryWriter = new BinaryWriter(s))
                  {
                      binaryWriter.Write((byte)reliability);

                      binaryWriter.Write(_clock.Clock);
                      binaryWriter.Write(eventSource.Id);

                      if (targetted)
                      {
                          var targetId = eventTarget != null ? eventTarget.Id : 0;

                          binaryWriter.Write(targetId);
                      }

                      binaryWriter.Write(eventId);
                      writer(s);
                  }
              });
        }

        private void OnUntargettedEvent(Packet<IScenePeer> packet)
        {
            MainThread.Post(() =>
                   {
                       using (var reader = new BinaryReader(packet.Stream))
                       {
                           var timeStamp = reader.ReadInt64();

                           StormancerNetworkIdentity source;

                           if (!TryGetSynchedObject(reader.ReadUInt32(), out source))
                           {
                               return;
                           }

                           var eventId = reader.ReadByte();


                           source.GetComponent<EventSender>().ReceiveEvent(eventId, timeStamp, packet.Stream);

                       }
                   });
        }

        private void OnTargettedEvent(Packet<IScenePeer> packet)
        {
            MainThread.Post(() =>
            {
                using (var reader = new BinaryReader(packet.Stream))
                {
                    var timeStamp = reader.ReadInt64();

                    StormancerNetworkIdentity source;
                    if (!TryGetSynchedObject(reader.ReadUInt32(), out source))
                    {
                        return;
                    }

                    var targetId = reader.ReadUInt32();

                    StormancerNetworkIdentity target = null;
                    if (targetId != 0)
                    {
                        TryGetSynchedObject(targetId, out target);
                    }
                    var eventId = reader.ReadByte();

                    source.GetComponent<EventSender>().ReceiveEvent(eventId, target, timeStamp, packet.Stream);
                }
            });
        }
    }
}
