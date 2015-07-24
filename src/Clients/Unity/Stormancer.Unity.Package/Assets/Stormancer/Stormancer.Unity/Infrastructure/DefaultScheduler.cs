using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniRx;

namespace Stormancer.Infrastructure
{
    public class DefaultScheduler : IScheduler
    {
        private UniRx.IScheduler _scheduler = Scheduler.ThreadPool;
        public IDisposable SchedulePeriodic(int delay, Action action)
        {

            if (delay <= 0)
            {
                throw new ArgumentOutOfRangeException("delay");
            }
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(delay))
                .SubscribeOn(this._scheduler)
                .Subscribe(_ => action()); 
        }
    }
}
