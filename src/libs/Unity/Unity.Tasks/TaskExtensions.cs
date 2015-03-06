using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        private static Task<TResult> ThenImpl<TResult>(Task task, Func<TResult> continuation)
        {
            var tcs = new TaskCompletionSource<TResult>();
            task.ContinueWith((Task t) =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    try
                    {
                        tcs.SetResult(continuation());
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }
            });
            return tcs.Task;
        }

        private static Task<TResult> ThenImpl<T, TResult>(this Task<T> task, Func<T, TResult> continuation)
        {
            var tcs = new TaskCompletionSource<TResult>();

            task.ContinueWith((Task<T> t) =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    try
                    {
                        tcs.SetResult(continuation(t.Result));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }
            });


            return tcs.Task;
        }

        public static Task Then(this Task task, Action continuation)
        {
            return ThenImpl(task, () =>
            {
                continuation();
                return true;
            });
        }

        public static Task<TResult> Then<TResult>(this Task task, Func<TResult> continuation)
        {
            return ThenImpl(task, continuation);
        }

        public static Task Then<T>(this Task<T> task, Action<T> continuation)
        {
            return ThenImpl(task, (T r) =>
            {
                continuation(r);
                return true;
            });
        }

        public static Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, TResult> continuation)
        {
            return ThenImpl(task, continuation);
        }

        public static Task Then(this Task task, Func<Task> continuation)
        {
            return ThenImpl(task, continuation).Unwrap();
        }

        public static Task<TResult> Then<TResult>(this Task task, Func<Task<TResult>> continuation)
        {
            return ThenImpl(task, continuation).Unwrap();
        }

        public static Task<TResult> Then<T,TResult>(this Task<T> task, Func<T,Task<TResult>> continuation)
        {
            return ThenImpl(task, continuation).Unwrap();
        }
    }
}
