using UnityEngine;
using System.Collections;
using Stormancer.Unity.Models;
using System;
using System.Collections.Concurrent;
using UniRx;
using System.Threading.Tasks;

namespace Stormancer
{
    public class SyncPlayersBehaviour : MonoBehaviour
    {
        // The local Player
        public GameObject Player;

        // The prefab used for other players
        public GameObject PlayerPrefab;
        public float _syncPeriod = 0.1f;

        private StormancerSceneBehaviour _stormancerScene;

        // Use this for initialization
        void Start()
        {
            this._stormancerScene = this.GetComponent<StormancerSceneBehaviour>();
            this._stormancerScene.ConfigureScene(InitScene);

            this._stormancerScene.ConnectedTask
                .Then(() =>
            {           
                        MainThreadDispatcher.Post(() =>
                {
                    this.InvokeRepeating("SendPosition", 0, this._syncPeriod);
                });
            });
        }   

        private void InitScene(Scene scene)
        {
            scene.RegisterRoute<PositionUpdate>("game.position", pos => 
            {
                if (this._stormancerScene.Id != null && pos.Id != this._stormancerScene.Id)
                {
                    MainThreadDispatcher.Post(() => 
                    {
                        var otherplayer = GameObject.Find(pos.Id.ToString());
                        
                        Interpolation interpolation;
                        if (otherplayer == null)
                        {
                            Debug.Log("Creating other player " + pos.Id);
                            otherplayer = (GameObject)Instantiate(PlayerPrefab);
                            otherplayer.name = pos.Id.ToString();
                            otherplayer.transform.position = pos.Position;
                            otherplayer.transform.rotation = pos.Rotation;
                            interpolation = otherplayer.GetComponent<Interpolation>();
                            if (interpolation != null)
                            {
                                interpolation.UpdatePeriod = this._syncPeriod;
                            }
                        }
                        
                        interpolation = otherplayer.GetComponent<Interpolation>();
                        if (interpolation != null)
                        {
                            interpolation.Move(pos.Position, pos.Rotation);
                        } else
                        {
                            otherplayer.transform.position = pos.Position;
                            otherplayer.transform.rotation = pos.Rotation;
                        }
                    });
                }
            });
            
            scene.RegisterRoute<string>("game.disconnected", id =>
            {
                MainThreadDispatcher.Post(() =>
                {
                    var otherplayer = GameObject.Find(id);
                    if (otherplayer != null)
                    {
                        Destroy(otherplayer);
                    }
                });
            });
            
            Debug.Log("Scene initialized for player synchronization");
        }

        public void SendPosition()
        {
            var transform = this.Player.transform;
            this._stormancerScene.Scene.Send("game.position", new PositionUpdate
                              {
                Position = (DtoVector) transform.position,
                Rotation = (DtoQuaternion) transform.rotation
            });
        }
    }
}

