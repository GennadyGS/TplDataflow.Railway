﻿using System;
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

        public static IEnumerable<Result<TOutput, TFailure>> SelectSafe<TInput, TOutput, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, Result<TOutput, TFailure>> selector)
        {
            return source.Select(item => item.SelectSafe(selector));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectManySafe<TInput, TOutput, TFailure>(
            this IEnumerable<Result<TInput, TFailure>> source,
            Func<TInput, IEnumerable<Result<TOutput, TFailure>>> selector)
        {
            return source.SelectMany(item => item.SelectManySafe(selector));
        }

        public static IEnumerable<Result<IList<T>, TFailure>> ToList<T, TFailure>(this IEnumerable<Result<T, TFailure>> source)
        {
            return source
                .GroupBy(item => item.IsSuccess)
                .SelectMany(group => group.Key
                    ? Enumerable.Repeat(Result.Success<IList<T>, TFailure>(@group.Select(item => item.Success).ToList()), 1)
                    : group.Select(item => Result.Failure<IList<T>, TFailure>(item.Failure)));
        }

        public static IEnumerable<TOutput> Match<TInput, TOutput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
            Func<IEnumerable<TInput>, TOutput> selectorOnSuccess, Func<IEnumerable<TFailure>, TOutput> selectorOnFailure)
        {
            return source
                .GroupBy(item => item.IsSuccess)
                .Select(group => group.Key
                    ? selectorOnSuccess(group.Select(item => item.Success))
                    : selectorOnFailure(group.Select(item => item.Failure)));
        }

        public static void Match<TInput, TFailure>(this IEnumerable<Result<TInput, TFailure>> source,
            Action<IEnumerable<TInput>> actionOnSuccess, Action<IEnumerable<TFailure>> actionOnFailure)
        {
            Match(source, actionOnSuccess.ToFunc(), actionOnFailure.ToFunc());
        }

        public static IEnumerable<TOutput> Map<TInput, TOutput>(this IEnumerable<TInput> source, Predicate<TInput> predicate,
            Func<IEnumerable<TInput>, TOutput> selectorOnTrue, Func<IEnumerable<TInput>, TOutput> selectorOnFalse)
        {
            return source
                .GroupBy(item => predicate(item))
                .Select(group => group.Key
                    ? selectorOnTrue(group)
                    : selectorOnFalse(group));
        }

        public static void Map<T>(this IEnumerable<T> source, Predicate<T> predicate,
            Action<IEnumerable<T>> actionOnTrue, Action<IEnumerable<T>> actionOnFalse)
        {
            source.Map(predicate, actionOnTrue.ToFunc(), actionOnFalse.ToFunc());
        }

        public static void LinkTo<T>(this IEnumerable<T> source, IObserver<T> target)
        {
            // TODO: Decouple from observable
            source.ToObservable().Subscribe(target);
        }

        public static IEnumerable<T> SideEffect<T>(this IEnumerable<T> source, Action<T> sideEffect)
        {
            return source.Select(item =>
            {
                sideEffect(item);
                return item;
            });
        }
    }
}