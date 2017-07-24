using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniRx;

namespace System.Threading.Tasks
{
    public static class ObservableExtensions
    {
        /// <summary>
        /// Converts the IObservable to a Task.
        /// </summary>
        /// <typeparam name="T">The return type of the source IObservable.</typeparam>
        /// <param name="source">The observable to convert.</param>
        /// <returns>A task completing or getting faulted at the same time as the observable.</returns>
        /// <remarks>The tasks completes when the converted observable completes, regardless of how many elements it had before completing.</remarks>
        public static Task ToVoidTask<T>(this IObservable<T> source, CancellationToken token)
        {
            return SubscribeAndCleanUp<T, Unit>(source,
                (IObservable<T> obs,TaskCompletionSource<Unit>  tcs) => obs.Subscribe(
                    (T t) => { },
                    (Exception ex) => tcs.SetException(ex),
                    () => tcs.SetResult(Unit.Default)), token);
        }

        /// <summary>
        /// Converts the IObvervable to a Task
        /// </summary>
        /// <typeparam name="T">The return type of the source IObservable.</typeparam>
        /// <param name="source">The observable to convert.</param>
        /// <returns>A task completing or getting faulted at the same type as the observable source.
        /// Its return value is the last value of the sequence.</returns>
        /// <remarks>
        /// The task completes when the converted observable completes, regardless of how many elements it had before completing.
        /// The task's return value is the last element of the sequence.
        /// The task is faulted with an InvalidOperationException if the sequence has no element.
        /// </remarks>
        public static Task<T> ToTask<T>(this IObservable<T> source, CancellationToken token)
        {
            var hasResult = false;
            T result = default(T);
            return SubscribeAndCleanUp<T, T>(source,
                (IObservable<T> obs, TaskCompletionSource<T> tcs) => obs.Subscribe(
                    (T t) =>
                    {
                        hasResult = true;
                        result = t;
                    },
                    (Exception ex) => tcs.SetException(ex),
                    () =>
                    {
                        if (hasResult)
                        {
                            tcs.SetResult(result);
                        }
                        else
                        {
                            tcs.SetException(new InvalidOperationException("Sequence has no element."));
                        }
                    }), token
                );
        }

        private static Task<TResult> SubscribeAndCleanUp<TData, TResult>(
            IObservable<TData> observable,
            Func<IObservable<TData>, TaskCompletionSource<TResult>, IDisposable> subscriptionMethod, CancellationToken token)
        {
         
            var tcs = new TaskCompletionSource<TResult>();

            var subscription = subscriptionMethod(observable, tcs);

            IDisposable tokenRegistration=null;
            tcs.Task.ContinueWith((Task<TResult> t) =>
            {
                if(subscription != null)
                {
                    subscription.Dispose();
                    subscription = null;
                }
                if(tokenRegistration != null)
                {
                    tokenRegistration.Dispose();
                    tokenRegistration = null;
                }
            });

            token.Register(() =>
            {
                if (subscription != null)
                {
                    subscription.Dispose();
                    subscription = null;
                }
                if (tokenRegistration != null)
                {
                    tokenRegistration.Dispose();
                    tokenRegistration = null;
                }
            });

            return tcs.Task;
        }
    }
}
