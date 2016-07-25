using Dataflow.Core;
using LanguageExt;
using Railway.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Dataflow.Railway
{
    public static class DataflowExtensions
    {
        public static IDataflow<TRight> Rights<TLeft, TRight>(this IDataflow<Either<TLeft, TRight>> source)
        {
            return source.Select(item => item.GetRightSafe());
        }

        public static IDataflow<TLeft> Lefts<TLeft, TRight>(this IDataflow<Either<TLeft, TRight>> source)
        {
            return source.Select(item => item.GetLeftSafe());
        }

        public static IDataflow<Either<TLeft, TRightOutput>> BindSafe<TLeft, TRightInput, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IDataflow<Either<TLeft, TRightOutput>>> bindFunc)
        {
            return source.Bind(item => 
                item.Match(
                    right => bindFunc(right), 
                    left => source.Factory.Return(Left<TLeft, TRightOutput>(left))));
        }


        public static IDataflow<Either<TLeft, TRightOutput>> Select<TLeft, TRightInput, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, TRightOutput> selector)
        {
            return source.Select(item => item.Select(selector));
        }

        public static IDataflow<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return source.SelectMany(item => item.SelectMany(selector));
        }

        public static IDataflow<Either<TLeft, TRightOutput>> SelectMany
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => EitherExtensions.SelectMany(item, mediumSelector, resultSelector));
        }

        public static IDataflow<Either<TLeft, TRightOutput>> SelectSafe<TLeft, TRightInput, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Either<TLeft, TRightOutput>> selector)
        {
            return source.Select(item => item.SelectSafe(selector));
        }

        public static IDataflow<Either<TLeft, TRightOutput>> SelectSafe
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Either<TLeft, TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.Select(item => item.SelectSafe(mediumSelector, resultSelector));
        }

        public static IDataflow<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> selector)
        {
            return source.SelectMany(item => item.SelectManySafe(selector));
        }

        public static IDataflow<Either<TLeft, TRightOutput>> SelectManySafe
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightMedium>>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectManySafe(mediumSelector, resultSelector));
        }

        public static IDataflow<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IDataflow<Either<TLeft, TRightOutput>>> bindFunc)
        {
            return source.BindSafe(bindFunc);
        }

        public static IDataflow<Either<TLeft, TRightOutput>> SelectManySafeAsync<TLeft, TRightInput, TRightOutput>(
            this IDataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Task<IEnumerable<Either<TLeft, TRightOutput>>>> selector)
        {
            throw new NotImplementedException();
        }

        public static IDataflow<Either<TLeft, IGroupedDataflow<TKey, TRight>>> GroupBySafe<TLeft, TRight, TKey>(
            this IDataflow<Either<TLeft, TRight>> source, Func<TRight, TKey> keySelector)
        {
            return source
                .GroupBy(item => item.IsRight)
                .SelectMany(
                    group => group.Key
                        ? group
                            .Rights()
                            .GroupBy(keySelector)
                            .Select(Right<TLeft, IGroupedDataflow<TKey, TRight>>)
                        : group
                            .Lefts()
                            .Select(Left<TLeft, IGroupedDataflow<TKey, TRight>>));
        }

        public static IDataflow<Either<TLeft, IList<TRight>>> BufferSafe<TLeft, TRight>(
            this IDataflow<Either<TLeft, TRight>> source, TimeSpan batchTimeout, int count)
        {
            return source
                .Buffer(batchTimeout, count)
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

        public static IDataflow<Either<TLeftOutput, TRightOutput>> Use<TInput, TLeftOutput, TRightOutput>(TInput disposable,
            Func<TInput, IDataflow<Either<TLeftOutput, TRightOutput>>> selector) where TInput : IDisposable
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