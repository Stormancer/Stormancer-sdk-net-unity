using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    /// <summary>
    /// A scheduler used to schedule network tasks
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Schedule a periodic task on the scheculder
        /// </summary>
        /// <param name="delay">Delay in ms between subsequent task runs.</param>
        /// <param name="action">An action to run. </param>
        IDisposable SchedulePeriodic(int delay, Action action);
    }
}
