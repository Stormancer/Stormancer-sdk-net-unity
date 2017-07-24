using Stormancer;
using System;
using System.Threading.Tasks;

namespace Stormancer
{
    public class ServerClock
    {
        private IClock _clock;
        private Scene _scene;
        private long _knwonServerTimeTicks;
        private DateTime _knownServerTime;

        public ServerClock(Scene scene)
        {
            this._scene = scene;
            this._clock = scene.DependencyResolver.Resolve<IClock>();
        }

        public bool IsSynched
        {
            get
            {
                return _tcs.Task.IsCompleted;
            }
        }
        public DateTime ServerTime
        {
            get
            {
                if (!IsSynched)
                {
                    throw new InvalidOperationException("unsynched with the server.");
                }
                return _knownServerTime + TimeSpan.FromMilliseconds(_clock.Clock - _knwonServerTimeTicks);
            }
        }

        private TaskCompletionSource<bool> _tcs;
        public Task<DateTime> ServerTimeAsync
        {
            get
            {
                if (_tcs == null)
                {
                    SyncServerTimeImpl();
                }
                return _tcs.Task.Then(_ => ServerTime);
            }
        }

        public Task<DateTime> SyncServerTime()
        {
            SyncServerTimeImpl();
            return ServerTimeAsync;
        }

        private void SyncServerTimeImpl()
        {
            if (_tcs == null)
            {
                _tcs = new TaskCompletionSource<bool>();
                _scene.RpcTask("clock.time").Then(packet =>
                {
                    _knwonServerTimeTicks = packet.ReadObject<long>();
                    _knownServerTime = packet.ReadObject<DateTime>();

                    _tcs.SetResult(true);
                })
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _scene.DependencyResolver.Resolve<ILogger>().Error(t.Exception);
                    }
                });
            }
        }
    }
}