using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Stormancer;
using System;
using System.Threading.Tasks;
using Stormancer.Plugins;

public static class ClientProvider
{
    private static ClientProviderImpl _instance;
    private static ClientProviderImpl Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ClientProviderImpl();
            }
            return _instance;
        }
    }

    public static T GetService<T>()
    {
        return Instance.GetService<T>();
    }

    public static Task<Scene> GetPublicScene<T>(string sceneid, T data)
    {
        return Instance.GetPublicScene<T>(sceneid, data);
    }

    public static Task<Scene> GetPrivateScene(string token)
    {
        return Instance.GetPrivateScene(token);
    }


    public static void DisconnectScene(string SceneId)
    {
        Instance.DisconnectScene(SceneId);
    }

    public static void ActivateAuthenticationPlugin()
    {
        Instance.AuthenticationPlugin = true;
    }

    public static void ActivateMatchmakingPlugin()
    {
        Instance.MatchmakingPlugin = true;
    }

    public static void ActivateDebugLog()
    {
        Instance.DebugLog = true;
    }

    public static void ActivateTransactionBrokerPlugin()
    {
        Instance.TransactionBroker = true;
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
        Instance.ApplicationName = str;
    }

    public static void SetServerEndpoint(string serverEndpoint)
    {
        Instance.ServerEndpoint = serverEndpoint;
    }

    public static void CloseClient()
    {
        Instance.CloseClient();
    }

    private class ClientProviderImpl
    {
        public string AccountId { get; set; }
        public string ApplicationName { get; set; }
        public string ServerEndpoint { get; set; }
        public bool AuthenticationPlugin { get; set; }
        public bool DebugLog { get; set; }
        public bool MatchmakingPlugin { get; set; }
        public bool TransactionBroker { get; set; }

        private Client _client;
        private ConcurrentDictionary<string, Scene> _scenes = new ConcurrentDictionary<string, Scene>();


        public long GetClientId()
        {
            if (_client == null)
            {
                return 0;
            }
            return _client.Id.Value;
        }

        public Task<Scene> GetPublicScene<T>(string sceneId, T data)
        {
            if (string.IsNullOrEmpty(sceneId))
            {
                Debug.LogWarning("SceneID can't be empty, cannot connect to remote scene");
                return TaskHelper.FromResult<Scene>(null);
            }
            if (_client == null)
            {
                InitClient();
                if (_client == null)
                {
                    return TaskHelper.FromResult<Scene>(null);
                }
            }
            if (_scenes.ContainsKey(sceneId) == true)
            {
                Debug.LogWarning("the scene " + sceneId + " have already been retrieved");
                return TaskHelper.FromResult<Scene>(null);
            }
            return _client.GetPublicScene(sceneId, data).ContinueWith(t =>
            {
                if (t.IsFaulted == true)
                {
                    Debug.LogException(t.Exception.InnerExceptions[0]);

                    return null;
                }
                Debug.Log("Retreived remote scene");
                _scenes.TryAdd(sceneId, t.Result);
                return t.Result;
            });
        }

        public Task<Scene> GetPrivateScene(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("token can't be empty, cannot connect to remote scene");
                return TaskHelper.FromResult<Scene>(null);
            }

            if (_client == null)
            {
                InitClient();
                if (_client == null)
                {
                    return TaskHelper.FromResult<Scene>(null);
                }
            }

            return _client.GetScene(token).ContinueWith(t =>
            {
                if (t.IsFaulted == true)
                {
                    Debug.LogException(t.Exception.InnerExceptions[0]);

                    return null;
                }
                Debug.Log("Retreived remote scene");
                _scenes.TryAdd(t.Result.Id, t.Result);
                return t.Result;
            });
        }

        private void InitClient()
        {
            if (string.IsNullOrEmpty(AccountId) || string.IsNullOrEmpty(ApplicationName))
            {
                Debug.LogError("AccountId or Application name are not set. Cannot connect to remoteScene");
            }
            var config = ClientConfiguration.ForAccount(AccountId, ApplicationName);

            if (!string.IsNullOrEmpty(ServerEndpoint))
            {
                config.ServerEndpoint = ServerEndpoint;
            }

            if (AuthenticationPlugin)
            {
                config.Plugins.Add(new AuthenticationPlugin());
            }
            if(MatchmakingPlugin)
            {
                config.Plugins.Add(new MatchmakingPlugin());
            }
            if(TransactionBroker)
            {
                config.Plugins.Add(new TransactionBrokerPlugin());
            }

            if (DebugLog)
            {
                config.Logger = DebugLogger.Instance;
            }

            _client = new Client(config);
            UniRx.MainThreadDispatcher.Initialize();
        }



        public void DisconnectScene(string sceneId)
        {
            Scene scene;

            if (_scenes.ContainsKey(sceneId) && _scenes.TryRemove(sceneId, out scene) == true)
            {
                scene.Disconnect();
            }
        }

        public void CloseClient()
        {
            using (_client)
            {
                _client = null;
            }
        }

        public T GetService<T>()
        {
            if (_client == null)
            {
                InitClient();
                if (_client == null)
                {
                    return default(T);
                }
            }

            return _client.DependencyResolver.Resolve<T>();
        }
    }
}
