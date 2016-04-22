using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;


namespace Stormancer
{
    public class MainThread : MonoBehaviour
    {
        public static void Post(Action action)
        {
            if (_instance != null)
            {
                if(!_isAppQuitting)
                {
                    Instance.PostImpl(action);
                }
            }
            else
            {
                throw new InvalidOperationException("Please use StormancerActionHandler.Initialize() in a behaviour before posting actions.");
            }
        }

        private static MainThread _instance;

        private static MainThread Instance
        {
            get
            {
                if (_isAppQuitting == true)
                    return null;
                return _instance;
            }

        }


        private static bool _isAppQuitting = false;
        private ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

        private void PostImpl(Action action)
        {
            if (_isAppQuitting == false)
                _actionQueue.Enqueue(action);
        }


        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject ActionHandler = new GameObject();
                _instance = ActionHandler.AddComponent<MainThread>();
                ActionHandler.name = "StormancerActionHandler";
                DontDestroyOnLoad(ActionHandler);
            }
        }

        void Update()
        {
            Action temp;
            while (_isAppQuitting == false && _actionQueue.Count > 0)
            {
                if (_actionQueue.TryDequeue(out temp))
                {
                    if (temp != null)
                    {
                        temp();
                    }
                }
            }
        }

        void OnApplicationQuit()
        {
            _isAppQuitting = true;
        }
    }
}
