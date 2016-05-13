using System;
using System.Collections.Generic;

namespace TplDataFlow.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IList<TSource>> Buffer<TSource>(this IEnumerable<TSource> source,
            TimeSpan timeSpan, int count)
        {
            throw new NotImplementedException();
        }

        public static Result<IEnumerable<TSuccess>, TFailure> ToResult<TSuccess, TFailure>(this IEnumerable<TSuccess> source)
        {
            return Result.Success<IEnumerable<TSuccess>, TFailure>(source);
        }

        public static Result<IEnumerable<TResult>, TFailure> SelectMany<TSource, TResult, TFailure>(this Result<IEnumerable<TSource>, TFailure> source,
            Func<TSource, Result<IEnumerable<TResult>, TFailure>> selector)
        {
            // return ResultExtensions.Select<IEnumerable<TSource>, TResult, TFailure>(source, selector);
            throw new NotImplementedException();
        }
    }
}