using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Rx.Extensions
{
    public static class ObservableExtensions
    {
        public static IObservable<TOutput> SelectAsync<TInput, TOutput>(this IObservable<TInput> source,
            Func<TInput, Task<TOutput>> selector)
        {
            return source.SelectMany(selector);
        }

        public static IObservable<TResult> SelectManyAsync<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, Task<IEnumerable<TResult>>> selector)
        {
            return source
                .SelectMany(selector)
                .SelectMany(items => items);
        }
   }
}