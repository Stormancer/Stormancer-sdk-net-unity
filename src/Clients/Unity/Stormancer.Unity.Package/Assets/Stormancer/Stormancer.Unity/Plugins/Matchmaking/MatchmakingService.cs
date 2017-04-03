using System;
using System.Threading.Tasks;
using Stormancer.Core;
using UniRx;
using Stormancer.Plugins.Matchmaking;

namespace Stormancer
{
    public class MatchmakingService
    {
        private readonly Scene _scene;
        private bool _isMatching;
        private IDisposable _matchmakingSubscription;

        public MatchState MatchState { get; private set; }

        public Action<MatchmakingResponse> OnMatchFound { get; set; }
        public Action<MatchState> OnMatchUpdate { get; set; }
        public Action<ReadyVerificationRequest> OnMatchReadyUpdate { get; set; }

        public MatchmakingService(Scene scene)
        {
            this._scene = scene;
            MatchState = MatchState.Unknown;

            scene.AddRoute("match.update", OnMatchUpdateCallback);
            scene.AddRoute("match.parameters.update", OnMatchParametersUpdateCallback);
            scene.AddRoute("match.ready.update", OnMatchReadyUpdateCallBack);
        }

        private void OnMatchUpdateCallback(Packet<IScenePeer> packet)
        {
            MatchState = (MatchState)packet.Stream.ReadByte();
            var onMatchUpdate = OnMatchUpdate;
            if (onMatchUpdate != null)
            {
                onMatchUpdate(MatchState);
            }

            if (MatchState == MatchState.Success)
            {
                var response = packet.ReadObject<MatchmakingResponse>();

                var onMatchFound = OnMatchFound;
                if (onMatchFound != null)
                {
                    onMatchFound(response);
                }
            }
        }

        private void OnMatchParametersUpdateCallback(Packet<IScenePeer> packet)
        {
            //not used
        }

        private void OnMatchReadyUpdateCallBack(Packet<IScenePeer> packet)
        {
            var readyUpdate = packet.ReadObject<ReadyVerificationRequestDto>().ToModel();

            var action = OnMatchReadyUpdate;
            if (action != null)
            {
                action(readyUpdate);
            }
        }

        public Task FindMatch(string provider)
        {
            var tcs = new TaskCompletionSource<bool>();

            _isMatching = true;

            var observable = _scene.Rpc("match.find", stream =>
            {
                _scene.Host.Serializer().Serialize(provider, stream);
            }, PacketPriority.MEDIUM_PRIORITY);

            Action<Packet<IScenePeer>> onNext = packet =>
            {
                using (_matchmakingSubscription)
                {
                    _isMatching = false;
                    _matchmakingSubscription = null;
                }
                tcs.SetResult(true);
            };

            Action<Exception> onError = exception =>
            {
                using (_matchmakingSubscription)
                {
                    _isMatching = false;
                    _matchmakingSubscription = null;
                }
                tcs.SetException(exception);
            };

            _matchmakingSubscription = observable.Subscribe(onNext, onError);

            return tcs.Task;
        }

        void Resolve(bool acceptMatch)
        {
            _scene.SendPacket("match.ready.resolve", stream =>
            {
                stream.WriteByte(acceptMatch ? (byte)1 : (byte)0);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
        }

        void Cancel()
        {
            if (_isMatching)
            {
                if (_matchmakingSubscription != null)
                {
                    _matchmakingSubscription.Dispose();
                    _matchmakingSubscription = null;
                }
                else
                {
                    _scene.SendPacket("match.cancel", _ => { });
                }
            }
        }
    }
}