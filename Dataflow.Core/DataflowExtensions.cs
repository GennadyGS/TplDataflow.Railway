using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public static class DataflowExtensions
    {
        public static IDataflow<TOutput> Select<TInput, TOutput>(this IDataflow<TInput> source,
            Func<TInput, TOutput> selector)
        {
            return source.Bind(item => source.Factory.Return(selector(item)));
        }

        public static IDataflow<TOutput> SelectMany<TInput, TOutput>(this IDataflow<TInput> source,
            Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.Bind(input => source.Factory.ReturnMany(selector(input)));
        }

        public static IDataflow<TOutput> SelectMany<TInput, TMedium, TOutput>(this IDataflow<TInput> dataflow,
            Func<TInput, IEnumerable<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return dataflow.SelectMany(input =>
                mediumSelector(input)
                    .Select(medium => resultSelector(input, medium)));
        }

        public static IDataflow<TOutput> SelectMany<TInput, TOutput>(this IDataflow<TInput> source,
            Func<TInput, IDataflow<TOutput>> selector)
        {
            return source.Bind(selector);
        }

        public static IDataflow<TOutput> SelectMany<TInput, TMedium, TOutput>(this IDataflow<TInput> source,
            Func<TInput, IDataflow<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.SelectMany(input =>
                mediumSelector(input)
                    .Select(medium => resultSelector(input, medium)));
        }

        public static IDataflow<IList<T>> Buffer<T>(this IDataflow<T> source,
            TimeSpan batchTimeout, int batchMaxSize)
        {
            return source.Bind(item => source.Factory.Buffer(item, batchTimeout, batchMaxSize));
        }
    }
}