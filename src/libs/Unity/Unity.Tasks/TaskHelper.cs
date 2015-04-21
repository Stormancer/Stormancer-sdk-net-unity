using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading.Tasks
{
    public static class TaskHelper
    {
        public static Task<T> FromResult<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static Task<T> FromExceptions<T>(IEnumerable<Exception> exceptions)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(exceptions);
            return tcs.Task;
        }

        public static Task If(bool condition, Func<Task> action)
        {
            if (condition)
            {
                return action();
            }
            else
            {
                return TaskHelper.FromResult(true);
            }
        }
    }
}
