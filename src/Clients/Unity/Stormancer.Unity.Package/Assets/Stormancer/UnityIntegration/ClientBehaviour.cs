using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Stormancer;
using System.Threading.Tasks;
using System;
using UnityEngine.Events;
using Stormancer.UnityIntegration;
using System.Linq;

namespace Stormancer
{
    public class ClientBehaviour : MonoBehaviour
    {
        public string AccountId = "";
        public string ApplicationName = "";
        public List<string> ServerEndpoints;

        [UnityEngine.Space]
        public bool DebugLog = false;
        public bool DisconnectOnDestroy = true;

        [UnityEngine.Header("Plugins")]
        public bool AuthenticationPlugin = false;
        public bool MatchmakingPlugin = false;
        public bool TransactionBrokerPlugin = false;
        public bool ServerClockPlugin = false;
        public bool LeaderboardPlugin = false;
        public bool ChatPlugin = false;
        public bool FriendsPlugin = false;
        public bool ClientSettings = false;
        public bool GameVersion = false;        

        public StringEvent OnDisconnected;

        void Awake()
        {
            ClientProvider.SetAccountId(AccountId);
            ClientProvider.SetApplicationName(ApplicationName);

            if (ServerEndpoints.Any())
            {
                ClientProvider.SetServerEndpoint(ServerEndpoints);
            }

            if (AuthenticationPlugin)
            {
                ClientProvider.ActivateAuthenticationPlugin();
            }
            if (MatchmakingPlugin)
            {
                ClientProvider.ActivateMatchmakingPlugin();
            }
            if (TransactionBrokerPlugin)
            {
                ClientProvider.ActivateTransactionBrokerPlugin();
            }

            if (ServerClockPlugin)
            {
                ClientProvider.ActivateServerClockPlugin();
            }

            if (LeaderboardPlugin)
            {
                ClientProvider.ActivateLeaderboardPlugin();
            }

            if (ChatPlugin)
            {
                ClientProvider.ActivateChatPlugin();
            }

            if(FriendsPlugin)
            {
                ClientProvider.ActivateFriendsPlugin();
            }

            if(ClientSettings)
            {
                ClientProvider.ActivateClientSettingsPlugin();
            }

            if(GameVersion)
            {
                ClientProvider.ActivateGameVersionPlugin();
            }

            if (DebugLog)
            {
                ClientProvider.ActivateDebugLog();
            }

            if (OnDisconnected != null)
            {
                ClientProvider.OnDisconnected += (s => MainThread.Post(() => OnDisconnected.Invoke(s)));
            }
        }

        private void OnDestroy()
        {
            if(DisconnectOnDestroy)
            {
                Debug.Log("Destroy!!");
                ClientProvider.CloseClient();
            }
        }
    }
}
