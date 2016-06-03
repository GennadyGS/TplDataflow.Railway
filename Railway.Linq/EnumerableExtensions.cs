﻿using LanguageExt;
using LanguageExt.Trans;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Railway.Linq
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<Either<TLeft, TRightOutput>> Select<TLeft, TRightInput, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, TRightOutput> selector)
        {
            return source.MapT(selector);
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return source.SelectMany(item => item.SelectMany(selector));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectMany
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectMany(mediumSelector, resultSelector));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectSafe<TLeft, TRightInput, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Either<TLeft, TRightOutput>> selector)
        {
            return source.Select(item => item.SelectSafe(selector));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectSafe
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Either<TLeft, TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.Select(item => item.SelectSafe(mediumSelector, resultSelector));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> selector)
        {
            return source.SelectMany(item => item.SelectManySafe(selector));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectManySafe
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightMedium>>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectManySafe(mediumSelector, resultSelector));
        }

        public static IEnumerable<Either<TLeft, IGrouping<TKey, TRight>>> GroupBy<TLeft, TRight, TKey>(
            this IEnumerable<Either<TLeft, TRight>> source, Func<TRight, TKey> keySelector)
        {
            return source
                .GroupBy(item => item.IsRight)
                .SelectMany(
                    group => group.Key
                        ? group
                            .Rights()
                            .GroupBy(keySelector)
                            .Select(Prelude.Right<TLeft, IGrouping<TKey, TRight>>)
                        : group
                            .Lefts()
                            .Select(Prelude.Left<TLeft, IGrouping<TKey, TRight>>));
        }

        public static IEnumerable<IList<T>> Buffer<T>(this IEnumerable<T> source, int count)
        {
            return source
                .Select((value, index) => new { value, index })
                .GroupBy(item => item.index / count)
                .Select(group => group.Select(item => item.value).ToList());
        }

        public static IEnumerable<Either<TLeft, IList<TRight>>> BufferSafe<TLeft, TRight>(
            this IEnumerable<Either<TLeft, TRight>> source, int count)
        {
            return source
                .Buffer(count)
                .SelectMany(batch => batch
                    .GroupBy(item => item.IsRight)
                    .SelectMany(group => group.Key
                        ? Prelude.List(
                            Prelude.Right<TLeft, IList<TRight>>(
                                group.Rights().ToList()))
                        : group
                            .Lefts()
                            .Select(Prelude.Left<TLeft, IList<TRight>>)));
        }
    }
}