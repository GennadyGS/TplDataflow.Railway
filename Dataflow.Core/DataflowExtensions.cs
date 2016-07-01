using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public static class DataflowExtensions
    {
        public static Dataflow<TOutput> Select<TInput, TOutput>(this Dataflow<TInput> source,
            Func<TInput, TOutput> selector)
        {
            return source.Bind(item => Dataflow.Return(selector(item)));
        }

        public static Dataflow<TOutput> SelectMany<TInput, TOutput>(this Dataflow<TInput> source,
            Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.Bind(input => Dataflow.ReturnMany(selector(input)));
        }

        public static Dataflow<TOutput> SelectMany<TInput, TMedium, TOutput>(this Dataflow<TInput> dataflow,
            Func<TInput, IEnumerable<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return dataflow.SelectMany(input =>
                mediumSelector(input)
                    .Select(medium => resultSelector(input, medium)));
        }

        public static Dataflow<TOutput> SelectMany<TInput, TOutput>(this Dataflow<TInput> source,
            Func<TInput, Dataflow<TOutput>> selector)
        {
            return source.Bind(selector);
        }

        public static Dataflow<TOutput> SelectMany<TInput, TMedium, TOutput>(this Dataflow<TInput> source,
            Func<TInput, Dataflow<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.SelectMany(input =>
                mediumSelector(input)
                    .Select(medium => resultSelector(input, medium)));
        }

        public static Dataflow<IList<T>> Buffer<T>(this Dataflow<T> source,
            TimeSpan batchTimeout, int batchMaxSize)
        {
            return source.Bind(item => Dataflow.Buffer<T>(item, batchTimeout, batchMaxSize));
        }
    }
}