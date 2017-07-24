using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Stormancer
{
    public class MainThread : MonoBehaviour
    {
        public static void Post(Action action)
        {
            if (_instance != null)
            {
                if (!_isAppQuitting)
                {
                    Instance.PostImpl(action);
                }
            }
            else if (!_isAppQuitting)
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
                GameObject go = new GameObject();
                _instance = go.AddComponent<MainThread>();
                go.name = "MainThread";
                DontDestroyOnLoad(go);
            }
        }

        void Update()
        {
            Action temp;
            while (_isAppQuitting == false && _actionQueue.TryDequeue(out temp))
            {
                if (temp != null)
                {
                    temp();
                }
            }
        }

        void OnApplicationQuit()
        {
            _isAppQuitting = true;
        }

        internal static Coroutine CoroutineFromTask(Task task)
        {
            return Instance.StartCoroutine(Instance.CoroutineFromTaskImpl(task));
        }

        private IEnumerator CoroutineFromTaskImpl(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
        }
    }
}
