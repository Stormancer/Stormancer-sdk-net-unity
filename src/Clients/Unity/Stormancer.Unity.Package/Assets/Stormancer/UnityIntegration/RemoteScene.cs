using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Stormancer;
using Stormancer.Core;
using System;
using System.Threading.Tasks;

namespace Stormancer
{
    public class RemoteScene : MonoBehaviour
    {
        public string SceneId = "";
        public bool ConnectOnLoad = true;
        public bool DisconnectOnUnLoad = true;
        public bool Connected = false;

        private List<RemoteLogicBase> _localLogics = new List<RemoteLogicBase>();
        public List<RemoteLogicBase> LocalLogics
        {
            get
            {
                return _localLogics;
            }
        }

        public Scene Scene { get; private set; }

        void Start()
        {
            if (ConnectOnLoad)
            {
                ConnectScene();
            }
        }

        public void ConnectScene()
        {
            ClientProvider.GetPublicScene(SceneId, "").Then(scene => InitSceneAndConnect(scene));
        }

        public void ConnectPrivateScene(string token)
        {
            ClientProvider.GetPrivateScene(token).Then(scene => InitSceneAndConnect(scene));
        }

        private void InitSceneAndConnect(Scene scene)
        {
            if (scene != null)
            {
                Scene = scene;
                foreach (RemoteLogicBase logic in LocalLogics)
                {
                    logic.Init(Scene);
                }

                Scene.Connect().ContinueWith(t =>
                {
                    if (Scene.Connected)
                    {
                        Debug.Log("connected to scene: " + SceneId);
                        Connected = true;
                        foreach (RemoteLogicBase remotelogic in LocalLogics)
                        {
                            remotelogic.OnConnected();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("failed to connect to scene: " + SceneId);
                    }
                });
            }

        }

        void Disconnect()
        {
            if (Scene != null && Scene.Connected)
            {
                ClientProvider.DisconnectScene(SceneId);
            }
        }
        

        void OnDestroy()
        {
            if (DisconnectOnUnLoad)
            {
                Disconnect();
            }
        }

        void OnApplicationQuit()
        {
            Disconnect();
            ClientProvider.CloseClient();
        }
    }
}
