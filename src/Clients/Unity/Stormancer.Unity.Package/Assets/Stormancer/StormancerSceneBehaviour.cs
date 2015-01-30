using UnityEngine;
using System.Collections;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Stormancer
{
    public class StormancerSceneBehaviour : MonoBehaviour
    {
        // Public fields
        public string AccountId;
        public string Application;
        public string SceneId;
        public bool UseLocalEmulator = false;
        private Scene _scene;

        public Scene Scene
        {
            get
            {
                return this._scene;
            }
        }
        public Client Client { get; private set; }

        private TaskCompletionSource<bool> _connectedTcs = new TaskCompletionSource<bool>();

        public Task ConnectedTask
        {
            get
            {
                return this._connectedTcs.Task;
            }
        }
       

        // Use this for initialization
        void Start()
        {
            ClientConfiguration config;
            if (this.UseLocalEmulator)
            {
                config = ClientConfiguration.ForLocalDev(this.Application);
            } else
            {
                config = ClientConfiguration.ForAccount(AccountId, Application);
            }

            Client = new Stormancer.Client(config);
            Client.GetPublicScene(this.SceneId, "")
                .ContinueWith<Scene>(task => {
                if (task.IsFaulted)
                {
                    Debug.LogException(task.Exception);
                }
                return task.Result;
            }).Then(scene =>
            {                    
                lock (this._configLock)
                {
                    this._scene = scene;
                    if (this._initConfig != null)
                    {
                        this._initConfig(this._scene);
                    }
                }
                return scene.Connect();
            })
                    .ContinueWith(t => 
            {
                if (t.IsFaulted)
                {
                    this._connectedTcs.SetException(t.Exception);
                } else
                {
                    Debug.Log("Stormancer scene connected");
                    this._connectedTcs.SetResult(true);
                }
            });
        }
   
        private object _configLock = new object();
        private Action<Scene> _initConfig = null;

        public void ConfigureScene(Action<Scene> configuration)
        {
            lock (_configLock)
            {
                if (this._scene != null)
                {
                    configuration(this._scene);
                } else
                {
                    this._initConfig += configuration;
                }
            }
        }       
    }
}
