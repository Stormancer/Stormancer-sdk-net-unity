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
        private Scene _scene;

        public Scene Scene
        {
            get
            {
                return this._scene;
            }
        }

        public long? Id { get { return this._client.Id; } }

        private Client _client;

        private TaskCompletionSource<Scene> _connectedTcs = new TaskCompletionSource<Scene>();

        public Task<Scene> ConnectedTask
        {
            get
            {
                return this._connectedTcs.Task;
            }
        }


        // Use this for initialization
        public Task<Scene> Connect()
        {
            ClientConfiguration config;
            config = ClientConfiguration.ForAccount(AccountId, Application);
            

            _client = new Stormancer.Client(config);
            _client.GetPublicScene(this.SceneId, "")
                .ContinueWith<Scene>(task =>
                {
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
                }
                else
                {
                    Debug.Log("Stormancer scene connected");
                    this._connectedTcs.SetResult(_scene);
                }
            });

            return this.ConnectedTask;
        }

        private object _configLock = new object();
        private Action<Scene> _initConfig = null;

        public void ConfigureScene(Action<Scene> configuration)
        {
            lock (_configLock)
            {
                if (this._scene != null && this._scene.Connected)
                {
                    throw new InvalidOperationException("You must configure the scene before it connects to the server.");
                }
                else
                {
                    this._initConfig += configuration;
                }
            }
        }

        Task _disconnectTask = null;
        public Task Disconnect()
        {
            if (this._disconnectTask == null)
            {
                this._disconnectTask = this.ConnectedTask.Then(scene => scene.Disconnect());
            }
            return this._disconnectTask;
        }

        public void OnDestroy()
        {
            this.Disconnect();
        }

        public void OnApplicationQuit()
        {
            this.Disconnect();
        }
    }
}
