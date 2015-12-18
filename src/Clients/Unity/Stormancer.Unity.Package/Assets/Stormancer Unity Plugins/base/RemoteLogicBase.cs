using UnityEngine;
using System.Collections;
using Stormancer;
using Stormancer.Core;

namespace Stormancer
{
    public abstract class RemoteLogicBase : MonoBehaviour
    {
        public RemoteScene RemoteScene;
        private long Clock
        {
            get
            {
                if (RemoteScene != null && RemoteScene.Scene != null)
                {
                    return RemoteScene.Scene.DependencyResolver.Resolve<IClock>().Clock;
                }
                throw new System.InvalidOperationException("Missing scene.");
            }
        }

        public void Awake()
        {
            if (RemoteScene == null)
                Debug.LogWarning("Remote has not been set on a remoteLogic !");
            else
                RemoteScene.LocalLogics.Add(this);
        }

        public abstract void Init(Scene s);
        public abstract void OnConnected();
    }
}
