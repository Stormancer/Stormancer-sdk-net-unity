using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Stormancer;

public static class ClientProvider
{
    private static ClientProviderImpl _Instance;
    private static ClientProviderImpl Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = new ClientProviderImpl();
            return _Instance;
        }
    }

    public static Scene GetPublicScene<T>(string sceneid, T data)
    {
        return Instance.GetPublicScene<T>(sceneid, data);
    }

    public static void DisconnectScene(string SceneId)
    {
        Instance.DisconnectScene(SceneId);
    }

    public static long GetClientId()
    {
        return Instance.GetClientId();
    }

    public static void SetAccountId(string str)
    {
        Instance.AccountId = str;
    }

    public static void SetApplicationName(string str)
    {
        Instance.AplplicationName = str;
    }

    private class ClientProviderImpl
    {
        public string AccountId = "";
        public string AplplicationName = "";
        private Client _Client;
        private ConcurrentDictionary<string, Scene> _scenes = new ConcurrentDictionary<string, Scene>();


        public long GetClientId()
        {
            if (_Client == null)
                return 0;
            return _Client.Id.Value;
        }

        public Scene GetPublicScene<T>(string sceneId, T data)
        {
            if (sceneId == "")
            {
                Debug.LogWarning("SceneID can't be empty, cannot connect to remote scene");
                return null;
            }
            if (_Client == null)
            {
                if (AccountId == "" || AplplicationName == "")
                {
                    Debug.LogError("AccountId or Application name are not set. Cannot connect to remoteScene");
                    return null;
                }
                var config = ClientConfiguration.ForAccount(AccountId, AplplicationName);
                _Client = new Client(config);
                UniRx.MainThreadDispatcher.Initialize();
            }
            if (_scenes.ContainsKey(sceneId) == true)
            {
                Debug.LogWarning("the scene " + sceneId + " have already been retrieved");
                return null;
            }
            return _Client.GetPublicScene(sceneId, data).ContinueWith(t =>
            {
                if (t.IsFaulted == true)
                {
                    Debug.LogWarning("connection Failed");
                    return null;
                }
                Debug.Log("Retreived remote scene");
                _scenes.TryAdd(sceneId, t.Result);
                return t.Result;
            }).Result;
        }

        public void DisconnectScene(string sceneId)
        {
            Scene scene;

            if (_scenes.ContainsKey(sceneId) && _scenes.TryRemove(sceneId, out scene) == true)
            {
                scene.Disconnect();
            }
        }
    }
}
