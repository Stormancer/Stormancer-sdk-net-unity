using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Core
{
    /// <summary>
    /// A list of event handler based on task factories.
    /// </summary>
    public interface ITaskBasedEventHandler
    {
        /// <summary>
        /// Adds a task to run when the event is fired.
        /// </summary>
        /// <param name="handler"></param>
        void Add(Func<Task> handler);
    }

    /// <summary>
    /// A list of event handler based on task factories taking an input value.
    /// </summary>
    /// <typeparam name="T">The type of the event handler input parameter.</typeparam>
    public interface ITaskBasedEventHandler<T>
    {
        /// <summary>
        /// Adds a task to run when the event is fired.
        /// </summary>
        /// <param name="handler"></param>
        void Add(Func<T, Task> handler);
    }
}
