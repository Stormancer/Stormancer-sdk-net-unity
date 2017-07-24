using Stormancer;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Stormancer.Plugins.GameSession;

namespace Stormancer
{
    public class GameSessionService
    {
        private readonly Scene _scene;
        private readonly TaskCompletionSource<bool> _waitServerTce = new TaskCompletionSource<bool>();
        private readonly ConcurrentDictionary<string, SessionPlayer> _users = new ConcurrentDictionary<string, SessionPlayer>();

        public IEnumerable<SessionPlayer> ConnectedPlayers
        {
            get
            {
                return _users.Values;
            }
        }

        public string SceneId
        {
            get
            {
                return _scene != null ? _scene.Id : null;
            }
        }

        public Action<SessionPlayer> OnConnectedPlayersChanged { get; set; }

        public GameSessionService(Scene scene)
        {
            this._scene = scene;

            _scene.AddRoute("server.started", packet =>
            {
                _waitServerTce.TrySetResult(true);
            });

            _scene.AddRoute<PlayerUpdate>("player.update", OnPlayerUpdate);
        }

        private void OnPlayerUpdate(PlayerUpdate update)
        {
            var player = new SessionPlayer(update.UserId, (PlayerStatus)update.Status);
            _users.AddOrUpdate(update.UserId, player, (_, __) => player);

            var action = OnConnectedPlayersChanged;
            if (action != null)
            {
                action(player);
            }
        }

        public Task WaitServerReady()
        {
            return _waitServerTce.Task;
        }

        public Task Connect()
        {
            return _scene.Connect();
        }

        public void Ready()
        {
            _scene.SendPacket("player.ready", _ => { });
        }

        public Task<TOut> SendGameResult<Tin, TOut>(Tin result)
        {
            return _scene.RpcTask<Tin, TOut>("gamesession.postresults", result);
        }

        public Task LeaveSession()
        {
            return _scene.Disconnect();
        }
    }
}