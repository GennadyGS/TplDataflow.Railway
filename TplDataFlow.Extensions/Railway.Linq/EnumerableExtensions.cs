using System;
using System.Collections.Generic;
using System.Linq;
using TplDataFlow.Extensions.Linq.Extensions;
using TplDataFlow.Extensions.Railway.Core;

namespace TplDataFlow.Extensions.Railway.Linq
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<Result<TOutput, TFailure>> Select<TInput, TOutput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, TOutput> selector)
        {
            return source.Select(item => item.Select(selector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectMany<TInput, TOutput, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.SelectMany(item => item.SelectMany(selector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectMany<TInput, TOutput, TMedium, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, IEnumerable<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectMany(mediumSelector, resultSelector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectSafe<TInput, TOutput, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, Result<TOutput, TFailure>> selector)
        {
            return source.Select(item => item.SelectSafe(selector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectSafe<TInput, TMedium,TOutput, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, Result<TMedium, TFailure>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.Select(item => item.SelectSafe(mediumSelector, resultSelector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectManySafe<TInput, TOutput, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, IEnumerable<Result<TOutput, TFailure>>> selector)
        {
            return source.SelectMany(item => item.SelectManySafe(selector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectManySafe<TInput, TMedium, TOutput, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, IEnumerable<Result<TMedium, TFailure>>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectManySafe(mediumSelector, resultSelector));
        }

        public static IEnumerable<Result<IGrouping<TKey, T>, TFailure>> GroupBy<T, TFailure, TKey>(
            this IEnumerable<Result<T, TFailure>> source, Func<T, TKey> keySelector)
        {
            return source
                .GroupBy(item => item.IsSuccess)
                .SelectMany(group => group.Key
                    ? group
                        .Select(item => item.Success)
                        .GroupBy(keySelector)
                        .Select(Result.Success<IGrouping<TKey, T>, TFailure>)
                    : group
                        .Select(item => item.Failure)
                        .Select(Result.Failure<IGrouping<TKey, T>, TFailure>));
        }

        public static IEnumerable<Result<IList<TSuccess>, TFailure>> BufferSafe<TSuccess, TFailure>(
            this IEnumerable<Result<TSuccess, TFailure>> source, int count)
        {
            return source
                .Buffer(count)
                .SelectMany(batch => batch
                    .GroupBy(item => item.IsSuccess)
                    .SelectMany(group => group.Key
                        ? Result.Success<IList<TSuccess>, TFailure>(group.Select(item => item.Success).ToList()).AsEnumerable()
                        : group.Select(item => item.Failure).Select(Result.Failure<IList<TSuccess>, TFailure>)));
        }
    }
}