using UnityEngine;
using System.Collections.Generic;
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
        private List<Scene> _scenes = new List<Scene>();

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
            return _Client.GetPublicScene(sceneId, data).ContinueWith(t =>
            {
                if (t.IsFaulted == true)
                {
                    Debug.LogWarning("connection Failed");
                    return null;
                }
                if (_scenes.Contains(t.Result) == true)
                {
                    Debug.LogWarning("the scene " + sceneId + " have already been retrieved");
                    return null;
                }
                Debug.Log("Retreived remote scene");
                _scenes.Add(t.Result);
                return t.Result;
            }).Result;
        }
    }
}
