using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniRx;

namespace System.Threading.Tasks
{
    public static class ObservableExtensions
    {
        public static Task ToVoidTask<T>(this IObservable<T> observable)
        {
            return SubscribeAndCleanUp<T, Unit>(observable,
                (obs, tcs) => obs.Subscribe(
                    t => { },
                    ex => tcs.SetException(ex),
                    () => tcs.SetResult(Unit.Default)));
        }

        public static Task<T> ToTask<T>(this IObservable<T> observable)
        {
            return SubscribeAndCleanUp<T, T>(observable.Single(),
                (obs, tcs) => obs.Subscribe(
                    t => tcs.SetResult(t),
                    ex => tcs.SetException(ex))
                );
        }

        private static Task<TResult> SubscribeAndCleanUp<TData, TResult>(
            IObservable<TData> observable,
            Func<IObservable<TData>, TaskCompletionSource<TResult>, IDisposable> subscriptionMethod)
        {
            var tcs = new TaskCompletionSource<TResult>();

            var subscription = subscriptionMethod(observable, tcs);

            tcs.Task.ContinueWith(t => subscription.Dispose());

            return tcs.Task;
        }
    }
}
