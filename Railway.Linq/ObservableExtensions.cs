﻿using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Rx.Extensions;
using static LanguageExt.Prelude;

namespace Railway.Linq
{
    public static class ObservableExtensions
    {
        public static IObservable<Either<TLeft, TRightOutput>> Select<TLeft, TRightInput, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, TRightOutput> selector)
        {
            return source.Select(item => item.Map(selector));
        }

        public static IObservable<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return source.SelectMany(item => item.SelectMany(selector));
        }

        public static IObservable<Either<TLeft, TRightOutput>> SelectMany
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectMany(mediumSelector, resultSelector));
        }

        public static IObservable<Either<TLeft, TRightOutput>> SelectSafe<TLeft, TRightInput, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Either<TLeft, TRightOutput>> selector)
        {
            return source.Select(item => item.SelectSafe(selector));
        }

        public static IObservable<Either<TLeft, TRightOutput>> SelectSafe
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Either<TLeft, TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.Select(item => item.SelectSafe(mediumSelector, resultSelector));
        }

        public static IObservable<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> selector)
        {
            return source.SelectMany(item => item.SelectManySafe(selector));
        }

        public static IObservable<Either<TLeft, TRightOutput>> SelectManySafe
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightMedium>>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectManySafe(mediumSelector, resultSelector));
        }

        public static IObservable<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IObservable<Either<TLeft, TRightOutput>>> selector)
        {
            return source.SelectMany(item =>
                item.Match(
                    right => selector(right),
                    left => Observable.Return<Either<TLeft, TRightOutput>>(left)));
        }

        public static IObservable<Either<TLeft, TRightOutput>> SelectManySafeAsync<TLeft, TRightInput, TRightOutput>(
            this IObservable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Task<IEnumerable<Either<TLeft, TRightOutput>>>> selector)
        {
            return source.SelectManyAsync(item =>
                item.Match(
                    right => selector(right), 
                    left => List(Left<TLeft, TRightOutput>(left))
                        .AsEnumerable()
                        .AsTask()));
        }

        public static IObservable<Either<TLeft, IGroupedObservable<TKey, TRight>>> GroupBySafe<TLeft, TRight, TKey>(
            this IObservable<Either<TLeft, TRight>> source, Func<TRight, TKey> keySelector)
        {
            return source
                .GroupBy(item => item.IsRight)
                .SelectMany(
                    group => group.Key
                        ? group
                            .SelectMany(item => item.RightAsEnumerable())
                            .GroupBy(keySelector)
                            .Select(Right<TLeft, IGroupedObservable<TKey, TRight>>)
                        : group
                            .SelectMany(item => item.LeftAsEnumerable())
                            .Select(Left<TLeft, IGroupedObservable<TKey, TRight>>));
        }

        public static IObservable<Either<TLeft, IList<TRight>>> BufferSafe<TLeft, TRight>(
            this IObservable<Either<TLeft, TRight>> source, int count)
        {
            return source
                .Buffer(count)
                .SelectMany(batch => batch
                    .GroupBy(item => item.IsRight)
                    .SelectMany(group => group.Key
                        ? List(
                            Right<TLeft, IList<TRight>>(
                                group.Rights().ToList()))
                        : group
                            .Lefts()
                            .Select(Left<TLeft, IList<TRight>>)));
        }

        public static IObservable<Either<TLeft, IList<TRight>>> BufferSafe<TLeft, TRight>(
            this IObservable<Either<TLeft, TRight>> source, TimeSpan timeSpan, int count)
        {
            return source
                .Buffer(timeSpan, count)
                .SelectMany(batch => batch
                    .GroupBy(item => item.IsRight)
                    .SelectMany(group => group.Key
                        ? List(
                            Right<TLeft, IList<TRight>>(
                                group.Rights().ToList()))
                        : group
                            .Lefts()
                            .Select(Left<TLeft, IList<TRight>>)));
        }

        public static IObservable<Either<TLeftOutput, TRightOutput>> Use<TInput, TLeftOutput, TRightOutput>(TInput disposable,
            Func<TInput, IObservable<Either<TLeftOutput, TRightOutput>>> selector) where TInput : IDisposable
        {
            return selector(disposable)
                .Select(item =>
                {
                    disposable.Dispose();
                    return item;
                });
        }
    }
}