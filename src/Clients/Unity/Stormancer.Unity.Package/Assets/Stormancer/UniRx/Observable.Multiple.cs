using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniRx
{
    public static partial class Observable
    {
        #region + Amb +

        private enum AmbState
        {
            Left,
            Right,
            Neither
        }

        /// <summary>
        /// Propagates the observable sequence that reacts first.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequences.</typeparam>
        /// <param name="first">First observable sequence.</param>
        /// <param name="second">Second observable sequence.</param>
        /// <returns>An observable sequence that surfaces either of the given sequences, whichever reacted first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first"/> or <paramref name="second"/> is null.</exception>
        //public static IObservable<TSource> Amb<TSource>(this IObservable<TSource> first, IObservable<TSource> second)
        //{
        //    if (first == null)
        //        throw new ArgumentNullException("first");
        //    if (second == null)
        //        throw new ArgumentNullException("second");

        //    return first.
        //}
        #endregion
    
    }
}
