using System;

namespace Stormancer
{
    /// <summary>
    /// An IDisposable implementation using an Action to provide the resource cleaning logic
    /// </summary>
    public class DisposableAction : IDisposable
    {
        private readonly Action _action;

        /// <summary>
        /// Creates a new DisposableAction instance
        /// </summary>
        /// <param name="action"></param>
        public DisposableAction(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// Disposes the resource.
        /// </summary>
        public void Dispose()
        {
            _action();
        }
    }
}