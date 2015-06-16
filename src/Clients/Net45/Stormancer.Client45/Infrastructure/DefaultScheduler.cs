using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stormancer.Infrastructure
{
    class DefaultScheduler: IScheduler
    {
        private System.Reactive.Concurrency.DefaultScheduler _scheduler = System.Reactive.Concurrency.DefaultScheduler.Instance;
        public IDisposable SchedulePeriodic(int delay, Action action)
        {
            if(delay <=0)
            {
                throw new ArgumentOutOfRangeException("delay");
            }
            if(action == null)
            {
                throw new ArgumentNullException("action");
            }

            return _scheduler.SchedulePeriodic(true, TimeSpan.FromMilliseconds(delay), (s) => {
                action();
                return s;
            });
        }
    }
}
