using Stormancer.Common.Helpers;
using Stormancer.Plugins.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public class AuthenticationService
    {
        private readonly Client _client;

        private bool _authenticated = false;
        private string _loginRoute = "login";
        private string _authenticationSceneName = "authenticator";
        private Task<Scene> _authenticationScene;

        public string UserId { get; private set; }
        public string UserName { get; private set; }


        public GameConnectionState ConnectionState { get; set; }
        public AuthenticationService(Client client)
        {
            this._client = client;
        }

        public Task<Scene> SteamLogin(string steamTicket)
        {
            var authContext = new Dictionary<string, string> { { "provider", "steam" }, { "ticket", steamTicket } };
            return Login(authContext);
        }

        public Task<Scene> DeviceIdentifierLogin()
        {
            var identifier = UnityEngine.SystemInfo.deviceUniqueIdentifier;

#if UNITY_EDITOR
            identifier = identifier + "editor";
#endif

            UnityEngine.Debug.Log(identifier);
            var authContext = new Dictionary<string, string> { { "provider", "deviceidentifier" }, { "deviceidentifier", identifier } };

            _client.DependencyResolver.Resolve<ILogger>().Log(Diagnostics.LogLevel.Debug, "authenticationservice", "Logging in with identifier " + identifier);
            return Login(authContext);
        }

        public Task<Scene> Login(Dictionary<string, string> authContext)
        {
            if (_authenticated)
            {
                return TaskHelper.FromException<Scene>(new InvalidOperationException("Already authenticated."));
            }

            return GetAuthenticationScene().Then(authScene =>
            {
                ConnectionState = GameConnectionState.Authenticating;

                return authScene.RpcTask<Dictionary<string, string>, LoginResult>(_loginRoute, authContext);
            })
            .Then(loginResult =>
            {
                if (loginResult.Success)
                {
                    ConnectionState = GameConnectionState.Authenticated;
                    UserId = loginResult.UserId;
                    UserName = loginResult.UserName;
                    return _client.GetScene(loginResult.Token);
                }
                else
                {
                    throw new Exception(loginResult.ErrorMsg);
                }
            });
        }

        public Task<Scene> GetPrivateScene(string sceneId)
        {
            return GetAuthenticationScene()
                .Then(authScene => authScene.RpcTask<string, string>("sceneauthorization.gettoken", sceneId))
                .Then(token => _client.GetScene(token));
        }

        private Task<Scene> GetAuthenticationScene()
        {
            if (_authenticationScene == null)
            {
                lock (this)
                {
                    if (_authenticationScene == null)
                    {
                        _authenticationScene = _client.GetPublicScene(_authenticationSceneName, "")
                            .Then(authScene =>
                            {
                                return authScene.Connect().Then(() => authScene);
                            });
                    }
                }
            }

            return _authenticationScene;
        }
    }
}
