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

        public List<GameObject> Prefabs;
        public List<StormancerNetworkIdentity> LocalObjectToSync;
        public ConcurrentDictionary<uint, StormancerNetworkIdentity> SlaveObjects = new ConcurrentDictionary<uint, StormancerNetworkIdentity>();
        public ConcurrentDictionary<uint, StormancerNetworkIdentity> MastersObjects = new ConcurrentDictionary<uint, StormancerNetworkIdentity>();

        private IClock Clock;

        public override void Init(Scene s)
        {
            if (s != null)
            {
                Debug.Log("replicator initializing");
                Clock = s.DependencyResolver.GetComponent<IClock>();
                s.AddProcedure("RequestObjects", OnRequestObjects);
                s.AddRoute("PlayerDisconnected", OnPlayerDisconnect);
                s.AddRoute("CreateObject", OnCreateObject);
                s.AddRoute("DestroyObject", OnDestroyObject);
                s.AddRoute("ForceUpdate", OnForceUpdate);
                s.AddRoute("UpdateObject", OnUpdateObject);
            }
        }

        public override void OnConnected()
        {
            Debug.Log("replicator connected");
            foreach (StormancerNetworkIdentity ni in LocalObjectToSync)
            {
                AddObjectToSynch(ni);
            }
        }

        public void AddObjectToSynch(StormancerNetworkIdentity ni)
        {
            Debug.Log("sending registration request");
            var dto = new ReplicatorDTO();
            dto.PrefabId = ni.PrefabId;
            dto.ClientId = ClientProvider.GetClientId();
            RemoteScene.Scene.RpcTask<ReplicatorDTO, ReplicatorDTO>("RegisterObject", dto).ContinueWith(response =>
            {
                Debug.Log("received registration");
                dto = response.Result;
                ni.Id = dto.Id;
                ni.MasterId = ClientProvider.GetClientId();
                MastersObjects.TryAdd(dto.Id, ni);
                if (SlaveObjects.ContainsKey(dto.Id) && UseDebugGhost == false)
                {
                    StormancerNetworkIdentity trash;
                    SlaveObjects.TryRemove(dto.Id, out trash);
                    MainThread.Post(() =>
                    {
                        Destroy(trash.gameObject);
                    });
                }
                else if (SlaveObjects.ContainsKey(dto.Id) == false && UseDebugGhost == true)
                {
                    MainThread.Post(() =>
                    {
                        var synchedGO = Instantiate(Prefabs[dto.PrefabId]);

                        var collider = synchedGO.GetComponent<Collider>();
                        if (collider != null)
                        {
                            collider.enabled = false;
                        }

                        var slave = synchedGO.GetComponent<StormancerNetworkIdentity>();
                        slave.Id = dto.Id;
                        slave.PrefabId = dto.PrefabId;
                        slave.MasterId = dto.ClientId;
                        SlaveObjects.TryAdd(dto.Id, slave);
                    });
                }
            });
        }

        public Task OnRequestObjects(RequestContext<IScenePeer> ctx)
        {
           List<ReplicatorDTO> dtos = new List<ReplicatorDTO>();

            foreach(StormancerNetworkIdentity ni in LocalObjectToSync)
            {
                ReplicatorDTO dto = new ReplicatorDTO();

                dto.Id = ni.Id;
                dto.PrefabId = ni.PrefabId;
                dto.ClientId = ClientProvider.GetClientId(); ;

                dtos.Add(dto);
            }
            ctx.SendValue<List<ReplicatorDTO>>(dtos);
            Debug.Log("receiving objects for new player. Sent " + dtos.Count + " objects.");
            return TaskHelper.FromResult(true);
        }

        public void OnPlayerDisconnect(Packet<IScenePeer> packet)
        {
            var clientId = packet.ReadObject<long>();
            int i = 0;

            foreach(StormancerNetworkIdentity ni in SlaveObjects.Values)
            {
                if (ni.MasterId == clientId)
                {
                    i++;
                    StormancerNetworkIdentity trash;
                    SlaveObjects.TryRemove(ni.Id, out trash);
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
            Debug.Log("removing object");
            var dto = new ReplicatorDTO();
            dto.Id = ni.Id;
            dto.ClientId = ClientProvider.GetClientId(); ;
            RemoteScene.Scene.Send<ReplicatorDTO>("RemoveObject", dto);
            MastersObjects.TryRemove(ni.Id, out ni);
        }

        private void OnCreateObject(Packet<IScenePeer> packet)
        {
            var dto = packet.ReadObject<ReplicatorDTO>();

            Debug.Log("creating object");
            if (dto.PrefabId < Prefabs.Count && SlaveObjects.ContainsKey(dto.Id) == false && MastersObjects.ContainsKey(dto.Id) == false)
            {
                MainThread.Post(() =>
                {
                    var SynchedGO = Instantiate(Prefabs[dto.PrefabId]);
                    var ni = SynchedGO.GetComponent<StormancerNetworkIdentity>();
                    ni.Id = dto.Id;
                    ni.PrefabId = dto.PrefabId;
                    ni.MasterId = dto.ClientId;
                    SlaveObjects.TryAdd(dto.Id, ni);
                });
            }
        }

        private void OnDestroyObject(Packet<IScenePeer> packet)
        {
            var dto = packet.ReadObject<ReplicatorDTO>();
            StormancerNetworkIdentity DestroyedGO;
            Debug.Log("destroying object");

            if (SlaveObjects.TryRemove(dto.Id, out DestroyedGO))
            {
                MainThread.Post(() =>
                {
                    Destroy(DestroyedGO.gameObject);
                });
            }
        }

        private void OnForceUpdate(Packet<IScenePeer> packet)
        {
            using (var reader = new BinaryReader(packet.Stream))
            {
                var id = reader.ReadUInt32();
                var SBid = reader.ReadByte();
                StormancerNetworkIdentity SO;

                if (MastersObjects.TryGetValue(id, out SO) && SBid < SO.SynchBehaviours.Count)
                {
                    MainThread.Post(() =>
                    {
                        SO.SynchBehaviours[SBid].ApplyChanges(packet.Stream);
                    });
                }
            }
        }

        private void OnUpdateObject(Packet<IScenePeer> packet)
        {
            using (var reader = new BinaryReader(packet.Stream))
            {
                var id = reader.ReadUInt32();
                var SBid = reader.ReadByte();
                StormancerNetworkIdentity SO;

                if (SlaveObjects.TryGetValue(id, out SO) && SBid < SO.SynchBehaviours.Count)
                {
                    SO.SynchBehaviours[SBid].ApplyChanges(packet.Stream);
                }
            }
        }

        void Update()
        {
            if (RemoteScene != null && RemoteScene.Scene != null && RemoteScene.Scene.Connected && MastersObjects.Count > 0)
            {
                foreach (KeyValuePair<uint, StormancerNetworkIdentity> kvp in MastersObjects)
                {
                    byte i = 0;
                    foreach (SyncBehaviourBase SB in kvp.Value.SynchBehaviours)
                    {
                        if (SB.LastSend + SB.timeBetweenUpdate < Clock.Clock)
                        {
                            SB.LastSend = Clock.Clock;
                            RemoteScene.Scene.SendPacket("UpdateSynchedObject", stream =>
                            {
                                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8))
                                {
                                    writer.Write((byte)SB.Reliability);
                                    writer.Write(kvp.Key);
                                    writer.Write(i);
                                    SB.SendChanges(stream);
                                }
                            }, PacketPriority.MEDIUM_PRIORITY, SB.Reliability);
                        }
                        i++;
                    }
                }
            }
        }
    }
}
