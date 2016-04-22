using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public static class TaskExtensions
    {
        public static Task FromException(Exception ex)
        {
            var tcs = new TaskCompletionSource<bool>();

            tcs.SetException(ex);

            return tcs.Task;
        }

        public static Task<TResult> FromException<TResult>(Exception ex)
        {
            var tcs = new TaskCompletionSource<TResult>();

            tcs.SetException(ex);

            return tcs.Task;
        }

        public static Task InvokeWrapping(this Func<Task> func)
        {
            try
            {
                return func();
            }
            catch  (Exception ex)
            {
                return FromException(ex);
            }
        }

        public static Task<TResult> InvokeWrapping<TResult>(this Func<Task<TResult>> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                return FromException<TResult>(ex);
            }
        }

        public static Task InvokeWrapping<TArg>(this Func<TArg, Task> func, TArg arg)
        {
            try
            {
                return func(arg);
            }
            catch (Exception ex)
            {
                return FromException(ex);
            }
        }

        public static Task<TResult> InvokeWrapping<TArg,TResult>(this Func<TArg, Task<TResult>> func, TArg arg)
        {
            try
            {
                return func(arg);
            }
            catch (Exception ex)
            {
                return FromException<TResult>(ex);
            }
        }
    }
}
