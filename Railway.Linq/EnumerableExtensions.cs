using Collection.Extensions;
using LanguageExt;
using LanguageExt.Trans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public static async Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectAsync<TLeft, TRightInput, TRightOutput>(
            this Task<IEnumerable<Either<TLeft, TRightInput>>> source,
            Func<TRightInput, TRightOutput> selector)
        {
            return (await source).MapT(selector);
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return source.SelectMany(item => item.SelectMany(selector));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectMany(mediumSelector, resultSelector));
        }

        public static Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectManyAsync<TLeft, TRightInput, TRightOutput>(
            this Task<IEnumerable<Either<TLeft, TRightInput>>> source,
            Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return source.SelectManyAsync(item => item.SelectMany(selector));
        }

        public static Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectManyAsync<TLeft, TRightInput, TRightMedium, TRightOutput>(
            this Task<IEnumerable<Either<TLeft, TRightInput>>> source,
            Func<TRightInput, IEnumerable<TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectManyAsync(item => item.SelectMany(mediumSelector, resultSelector));
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

        public static Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectSafeAsync<TLeft, TRightInput, TRightOutput>(
            this Task<IEnumerable<Either<TLeft, TRightInput>>> source,
            Func<TRightInput, Either<TLeft, TRightOutput>> selector)
        {
            return source.SelectAsync(item => item.SelectSafe(selector));
        }

        public static Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectSafeAsync<TLeft, TRightInput, TRightOutput>(
            this Task<IEnumerable<Either<TLeft, TRightInput>>> source,
            Func<TRightInput, Task<Either<TLeft, TRightOutput>>> selector)
        {
            return source.SelectAsync(item => item.SelectSafeAsync(selector));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> selector)
        {
            return source.SelectMany(item => item.SelectManySafe(selector));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IEnumerable<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightMedium>>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectManySafe(mediumSelector, resultSelector));
        }

        public static Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectManySafeAsync<TLeft, TRightInput, TRightOutput>(
            this Task<IEnumerable<Either<TLeft, TRightInput>>> source,
            Func<TRightInput, Task<IEnumerable<Either<TLeft, TRightOutput>>>> selector)
        {
            return source.SelectManyAsync(item => item.SelectManySafeAsync(selector));
        }

        public static IEnumerable<Either<TLeft, IGrouping<TKey, TRight>>> GroupBySafe<TLeft, TRight, TKey>(
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

        public static Task<IEnumerable<Either<TLeft, IGrouping<TKey, TRight>>>> GroupBySafeAsync<TLeft, TRight, TKey>(
            this Task<IEnumerable<Either<TLeft, TRight>>> source, Func<TRight, TKey> keySelector)
        {
            return source
                .GroupByAsync(item => item.IsRight)
                .SelectManyAsync(
                    group => group.Key
                        ? group
                            .Rights()
                            .GroupBy(keySelector)
                            .Select(Prelude.Right<TLeft, IGrouping<TKey, TRight>>)
                        : group
                            .Lefts()
                            .Select(Prelude.Left<TLeft, IGrouping<TKey, TRight>>));
        }

        public static IEnumerable<Either<TLeft, IList<TRight>>> BufferSafe<TLeft, TRight>(
            this IEnumerable<Either<TLeft, TRight>> source, TimeSpan batchTimeout, int count)
        {
            return source
                .Buffer(batchTimeout, count)
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

        public static async Task<IEnumerable<Either<TLeft, IList<TRight>>>> BufferSafeAsync<TLeft, TRight>(
            this Task<IEnumerable<Either<TLeft, TRight>>> source, TimeSpan batchTimeout, int count)
        {
            return (await source).BufferSafe(batchTimeout, count);
        }

        public static IEnumerable<Either<TLeftOutput, TRightOutput>> Use<TInput, TLeftOutput, TRightOutput>(TInput disposable,
            Func<TInput, IEnumerable<Either<TLeftOutput, TRightOutput>>> selector) where TInput : IDisposable
        {
            return selector(disposable)
                .Select(item =>
                {
                    disposable.Dispose();
                    return item;
                });
        }

        public static Task<IEnumerable<Either<TLeftOutput, TRightOutput>>> UseAsync<TInput, TLeftOutput, TRightOutput>(TInput disposable,
            Func<TInput, Task<IEnumerable<Either<TLeftOutput, TRightOutput>>>> selector) where TInput : IDisposable
        {
            return selector(disposable)
                .SelectAsync(item =>
                {
                    disposable.Dispose();
                    return item;
                });
        }
    }
}