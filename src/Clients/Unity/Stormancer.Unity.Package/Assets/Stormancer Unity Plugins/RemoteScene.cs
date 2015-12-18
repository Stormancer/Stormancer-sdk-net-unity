using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Stormancer;
using Stormancer.Core;

namespace Stormancer
{
    public class RemoteScene : MonoBehaviour
    {
        public string SceneId = "";
        public bool ConnectOnLoad = true;
        public bool DisconnectOnUnLoad = true;
        public bool Connected = false;

        private List<RemoteLogicBase> _LocalLogics;
        public List<RemoteLogicBase> LocalLogics
        { get
            {
                if (_LocalLogics == null)
                    _LocalLogics = new List<RemoteLogicBase>();
                return _LocalLogics;
            }
            private set { }
        }
        
        public Scene Scene;

        void Start()
        {
            if (ConnectOnLoad == true)
            {
                ConnectScene();
            }
        }

        public void ConnectScene()
        {
            Scene = ClientProvider.GetPublicScene(SceneId, "");
            if (Scene != null)
            {
                foreach (RemoteLogicBase logic in LocalLogics)
                {
                    logic.Init(Scene);
                }

                Scene.Connect().ContinueWith(t =>
                {
                    if (Scene.Connected == true)
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

        void onDestroy()
        {
            if (DisconnectOnUnLoad == true)
            {
                Disconnect();
            }
        }

        void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}
