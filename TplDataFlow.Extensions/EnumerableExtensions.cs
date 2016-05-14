using System;
using System.Collections.Generic;
using System.Linq;

namespace TplDataFlow.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IList<TSource>> Buffer<TSource>(this IEnumerable<TSource> source,
            TimeSpan timeSpan, int count)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<Result<TSuccess, TFailure>> ToResult<TSuccess, TFailure>(this IEnumerable<TSuccess> source)
        {
            return source.Select(Result.Success<TSuccess, TFailure>);
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectMany<TInput, TOutput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, Result<IEnumerable<TOutput>, TFailure>> selector)
        {
            //return System.Linq.Enumerable.SelectMany(source, i => i.SelectMany(selector));
            // return ResultExtensions.Select<IEnumerable<TSource>, TResult, TFailure>(source, selector);
            throw new NotImplementedException();
        }
    }
}