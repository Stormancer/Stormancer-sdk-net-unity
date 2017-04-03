using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Stormancer;
using System.Threading.Tasks;

namespace Stormancer
{
    public class ClientBehaviour : MonoBehaviour
    {
        public string AccountId = "";
        public string ApplicationName = "";
        public string ServerEndpoint;

        [UnityEngine.Space]
        public bool DebugLog = false;

        [UnityEngine.Header("Plugins")]
        public bool AuthenticationPlugin = false;
        public bool MatchmakingPlugin = false;
        public bool TransactionBrokerPlugin = false;



        void Awake()
        {
            ClientProvider.SetAccountId(AccountId);
            ClientProvider.SetApplicationName(ApplicationName);

            if (!string.IsNullOrEmpty(ServerEndpoint))
            {
                ClientProvider.SetServerEndpoint(ServerEndpoint);
            }

            if (AuthenticationPlugin)
            {
                ClientProvider.ActivateAuthenticationPlugin();
            }
            if(MatchmakingPlugin)
            {
                ClientProvider.ActivateMatchmakingPlugin();
            }
            if(TransactionBrokerPlugin)
            {
                ClientProvider.ActivateTransactionBrokerPlugin();
            }

            if(DebugLog)
            {
                ClientProvider.ActivateDebugLog();
            }
        }
    }
}
