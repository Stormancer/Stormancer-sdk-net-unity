using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Stormancer;
using System;
using System.Threading.Tasks;
using Stormancer.Plugins;
using System.Linq;

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

    public static Action<ClientConfiguration> OnClientConfiguration
    {
        get
        {
            return Instance.OnClientConfiguration;
        }
        set
        {
            Instance.OnClientConfiguration = value;
        }
    }

    public static T GetService<T>()
    {
        return Instance.GetService<T>();
    }

    public static Task<T> GetService<T>(string scene)
    {
        return Instance.GetService<T>(scene);
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

    public static void ActivateServerClockPlugin()
    {
        Instance.ServerClockPlugin = true;
    }

    public static void ActivateDebugLog()
    {
        Instance.DebugLog = true;
    }

    public static void ActivateTransactionBrokerPlugin()
    {
        Instance.TransactionBroker = true;
    }

    public static void ActivateLeaderboardPlugin()
    {
        Instance.LeaderboardPlugin = true;
    }

    public static void ActivateChatPlugin()
    {
        Instance.ChatPlugin = true;
    }

    public static void ActivateFriendsPlugin()
    {
        Instance.FriendsPlugin = true;
    }

    internal static void ActivateClientSettingsPlugin()
    {
        Instance.ClientSettingsPlugin = true;
    }

    internal static void ActivateGameVersionPlugin()
    {
        Instance.GameVersionPlugin = true;
    }

    public static event Action<string> OnDisconnected
    {
        add
        {
            Instance.OnDisconnected += value;
        }
        remove
        {
            Instance.OnDisconnected -= value;
        }
    }

 

    public static long ClientId
    {
        get
        {
            return Instance.GetClientId();
        }
    }

    public static void SetAccountId(string str)
    {
        Instance.AccountId = str;
    }

    public static void SetApplicationName(string str)
    {
        Instance.ApplicationName = str;
    }

    public static void SetServerEndpoint(List<string> serverEndpoints)
    {
        Instance.ServerEndpoints = serverEndpoints;
    }

    public static void CloseClient()
    {
        Instance.CloseClient();
    }

    private class ClientProviderImpl
    {
        public string AccountId
        {
            get; set;
        }
        public string ApplicationName
        {
            get; set;
        }
        public List<string> ServerEndpoints
        {
            get; set;
        }
        public bool AuthenticationPlugin
        {
            get; set;
        }
        public bool DebugLog
        {
            get; set;
        }
        public bool MatchmakingPlugin
        {
            get; set;
        }
        public bool ServerClockPlugin { get; internal set; }
        public bool TransactionBroker
        {
            get; set;
        }
        public bool LeaderboardPlugin { get; set; }
        public bool ChatPlugin { get; set; }
        public bool FriendsPlugin { get; internal set; }
        public bool ClientSettingsPlugin { get; internal set; }
        public bool GameVersionPlugin { get; internal set; }


        public Action<ClientConfiguration> OnClientConfiguration { get; set; }

        private Action<string> _onDisconnected;
        private void InvokeDisonnected(string reason)
        {
            if (_onDisconnected != null)
            {
                MainThread.Post(() =>
                {
                    var action = _onDisconnected;
                    if (action != null)
                    {
                        action(reason);
                    }
                });
            }
        }
        
        public event Action<string> OnDisconnected
        {
            add
            {
                _onDisconnected += value;
            }
            remove
            {
                _onDisconnected -= value;
            }
        }

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
            if (_scenes.ContainsKey(sceneId))
            {
                return TaskHelper.FromResult<Scene>(null);
            }
            return _client.GetPublicScene(sceneId, data).ContinueWith(t =>
            {
                if (t.IsFaulted)
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

            if (ServerEndpoints.Any())
            {
                config.ServerEndpoints = ServerEndpoints;
            }

            if (AuthenticationPlugin)
            {
                config.Plugins.Add(new AuthenticationPlugin());
            }
            if (MatchmakingPlugin)
            {
                config.Plugins.Add(new MatchmakingPlugin());
            }
            if (TransactionBroker)
            {
                config.Plugins.Add(new TransactionBrokerPlugin());
            }
            if (ServerClockPlugin)
            {
                config.Plugins.Add(new ServerClockPlugin());
            }
            if (LeaderboardPlugin)
            {
                config.Plugins.Add(new LeaderboardPlugin());
            }
            if (ChatPlugin)
            {
                config.Plugins.Add(new ChatPlugin());
            }
            if(FriendsPlugin)
            {
                config.Plugins.Add(new FriendsPlugin());
            }
            if(ClientSettingsPlugin)
            {
                config.Plugins.Add(new ClientSettingsPlugin());
            }
            if(GameVersionPlugin)
            {
                config.Plugins.Add(new GameVersionPlugin());
            }

            if (DebugLog)
            {
                config.Logger = DebugLogger.Instance;
            }

            var action = OnClientConfiguration;
            if (action != null)
            {
                action(config);
            }

            _client = new Client(config);
            _client.OnDisconnected += InvokeDisonnected;
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
                _scenes.Clear();
                _sceneTasks.Clear();
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

            T result;
            if (!_client.DependencyResolver.TryResolve<T>(out result))
            {
                return default(T);
            }

            return result;
        }


        private readonly ConcurrentDictionary<string, Task<Scene>> _sceneTasks = new ConcurrentDictionary<string, Task<Scene>>();
        internal Task<T> GetService<T>(string sceneId)
        {
            return _sceneTasks.GetOrAdd(sceneId, GetAndConnectScene).Then(scene => scene.DependencyResolver.Resolve<T>());
        }
        private Task<Scene> GetAndConnectScene(string sceneId)
        {
            Task<Scene> task;
            var authenticationService = GetService<AuthenticationService>();
            if (authenticationService != null)
            {
                task = authenticationService.GetPrivateScene(sceneId);
            }
            else
            {
                task = GetPublicScene(sceneId, "");
            }
            return task.Then(scene => scene.Connect().Then(() => scene));
        }
    }
}
