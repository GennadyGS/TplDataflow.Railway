using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

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
            return source
                .Select((value, index) => new { value, index })
                .GroupBy(item => item.index / count)
                .Select(group => group.Select(item => item.value).ToList());
        }

        public static IEnumerable<Result<IList<TSuccess>, TFailure>> BufferSafe<TSuccess, TFailure>(this IEnumerable<Result<TSuccess, TFailure>> source,
            TimeSpan timeSpan, int count)
        {
            return source
                .Buffer(timeSpan, count)
                .SelectMany(batch => batch
                    .GroupBy(item => item.IsSuccess)
                    .SelectMany(group => group.Key
                        ? Enumerable.Repeat(Result.Success<IList<TSuccess>, TFailure>(group.Select(item => item.Success).ToList()), 1)
                        : group.Select(item => Result.Failure<IList<TSuccess>, TFailure>(item.Failure))));
        }

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
            this IEnumerable<Result<T, TFailure>> source,
            Func<T, TKey> keySelector)
        {
            return source
                .Match(success => 
                    success
                        .GroupBy(keySelector)
                        .ToResult<IGrouping<TKey, T>, TFailure>(),
                    failure => failure.Select(Result.Failure<IGrouping<TKey, T>, TFailure>));
        }

        public static IEnumerable<Result<IList<T>, TFailure>> ToList<T, TFailure>(this IEnumerable<Result<T, TFailure>> source)
        {
            return source
                .GroupBy(item => item.IsSuccess)
                .SelectMany(group => group.Key
                    ? Enumerable.Repeat(Result.Success<IList<T>, TFailure>(@group.Select(item => item.Success).ToList()), 1)
                    : group.Select(item => Result.Failure<IList<T>, TFailure>(item.Failure)));
        }

        public static IEnumerable<TOutput> Match<TInput, TOutput, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<IEnumerable<TInput>, IEnumerable<TOutput>> selectorOnSuccess,
            Func<IEnumerable<TFailure>, IEnumerable<TOutput>> selectorOnFailure)
        {
            return source
                .GroupBy(item => item.IsSuccess)
                .SelectMany(group => group.Key
                    ? selectorOnSuccess(group.Select(item => item.Success))
                    : selectorOnFailure(group.Select(item => item.Failure)));
        }
        public static TResult Match<TInput, TInputFailure, TOutput, TOutputFailure, TResult>(
            this IEnumerable<Result<TInput, TInputFailure>> source,
            Func<IEnumerable<TInput>, TOutput> selectorOnSuccess,
            Func<IEnumerable<TInputFailure>, TOutputFailure> selectorOnFailure,
            Func<TOutput, TOutputFailure, TResult> resultSelector)
        {
            // TODO: Avoid multiple enumerations
            return resultSelector(
                selectorOnSuccess(source
                    .Where(item => item.IsSuccess)
                    .Select(item => item.Success)), 
                selectorOnFailure(source
                    .Where(item => !item.IsSuccess)
                    .Select(item => item.Failure)));
        }

        public static IEnumerable<TOutput> Map<TInput, TOutput>(this IEnumerable<TInput> source,
            Predicate<TInput> predicate,
            Func<IEnumerable<TInput>, IEnumerable<TOutput>> selectorOnTrue,
            Func<IEnumerable<TInput>, IEnumerable<TOutput>> selectorOnFalse)
        {
            return source
                .GroupBy(item => predicate(item))
                .SelectMany(group => group.Key
                    ? selectorOnTrue(group)
                    : selectorOnFalse(group));
        }

        public static TResult Map<TInput, TOutputTrue, TOutputFalse, TResult>(this IEnumerable<TInput> source,
            Predicate<TInput> predicate,
            Func<IEnumerable<TInput>, TOutputTrue> selectorOnTrue,
            Func<IEnumerable<TInput>, TOutputFalse> selectorOnFalse,
            Func<TOutputTrue, TOutputFalse, TResult> resultSelector)
        {
            // TODO: Avoid multiple enumerations
            return resultSelector(
                selectorOnTrue(source.Where(item => predicate(item))), 
                selectorOnFalse(source.Where(item => !predicate(item))));
        }

        public static void LinkTo<T>(this IEnumerable<T> source, IObserver<T> target)
        {
            // TODO: Decouple from observable
            source.ToObservable().Subscribe(target);
        }
    }
}