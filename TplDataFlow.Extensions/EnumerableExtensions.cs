using System;
using System.Collections.Generic;
using System.Linq;

namespace TplDataFlow.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<Result<T, TFailure>> ToResult<T, TFailure>(this IEnumerable<T> source)
        {
            return source.Select(Result.Success<T, TFailure>);
        }

        public static IEnumerable<IList<T>> Buffer<T>(this IEnumerable<T> source,
            TimeSpan timeSpan, int count)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<Result<IList<TInput>, TFailure>> Buffer<TInput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
            TimeSpan timeSpan, int count)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<Result<TOutput, TFailure>> Select<TInput, TOutput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
                Func<TInput, TOutput> selector)
        {
            return source.Select(item => item.Select(selector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectSafe<TInput, TOutput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
                Func<TInput, Result<TOutput, TFailure>> selector)
        {
            return source.Select(item => item.SelectSafe(selector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectMany<TInput, TOutput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
                Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.SelectMany(item => item.SelectManyFromResult(selector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectManySafe<TInput, TOutput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
                Func<TInput, Result<IEnumerable<TOutput>, TFailure>> selector)
        {
            return source.SelectMany(item => item.SelectManySafeFromResult(selector));
        }

        public static void Match<TSuccess, TFailure>(this IEnumerable<Result<TSuccess, TFailure>> source,
            Action<IEnumerable<TSuccess>> onSuccess, Action<IEnumerable<TFailure>> onFailure)
        {
            throw new NotImplementedException();
        }

        public static void Split<T>(this IEnumerable<T> source, Predicate<T> predicate,
            Action<IEnumerable<T>> onTrue, Action<IEnumerable<T>> onFalse)
        {
            throw new NotImplementedException();
        }

        // TODO: Refactoring
        private static IEnumerable<Result<TOutput, TFailure>> SelectManyFromResult<TInput, TOutput, TFailure>(this Result<TInput, TFailure> source, 
            Func<TInput, IEnumerable<TOutput>> selector)
        {
            if (!source.IsSuccess)
            {
                return Enumerable.Repeat(Result.Failure<TOutput, TFailure>(source.Failure), 1);
            }
            return selector(source.Success)
                .Select(item => item.ToResult<TOutput, TFailure>());
        }

        private static IEnumerable<Result<TOutput, TFailure>> SelectManySafeFromResult<TInput, TOutput, TFailure>(this Result<TInput, TFailure> source, 
            Func<TInput, Result<IEnumerable<TOutput>, TFailure>> selector)
        {
            if (!source.IsSuccess)
            {
                return Enumerable.Repeat(Result.Failure<TOutput, TFailure>(source.Failure), 1);
            }
            Result<IEnumerable<TOutput>, TFailure> res = selector(source.Success);
            return res.Match(success => success.Select<TOutput, Result<TOutput, TFailure>>(item => item.ToResult<TOutput, TFailure>()),
                failure => Enumerable.Repeat(Result.Failure<TOutput, TFailure>(failure), 1));
        }
    }
}