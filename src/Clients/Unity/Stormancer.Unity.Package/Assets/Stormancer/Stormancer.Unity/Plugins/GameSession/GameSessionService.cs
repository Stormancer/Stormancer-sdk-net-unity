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
        private readonly TaskCompletionSource<GameServerInformation> _waitServerTce = new TaskCompletionSource<GameServerInformation>();
        private readonly ConcurrentDictionary<string, SessionPlayer> _users = new ConcurrentDictionary<string, SessionPlayer>();

        public IEnumerable<SessionPlayer> ConnectedPlayers
        {
            get
            {
                return _users.Values;
            }
        }

        public Action<SessionPlayer> OnConnectedPlayersChanged { get; set; }

        public GameSessionService(Scene scene)
        {
            this._scene = scene;

            _scene.AddRoute<GameServerInformation>("server.started", serverInfo =>
            {
                _waitServerTce.TrySetResult(serverInfo);
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

        public Task<GameServerInformation> WaitServerReady()
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
    }
}