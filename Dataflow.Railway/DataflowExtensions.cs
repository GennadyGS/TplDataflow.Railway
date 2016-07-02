using System;
using System.Collections.Generic;
using System.Linq;
using Dataflow.Core;
using LanguageExt;
using Railway.Linq;

namespace Dataflow.Railway
{
    public static class DataflowExtensions
    {
        public static Dataflow<Either<TLeft, TRightOutput>> Select<TLeft, TRightInput, TRightOutput>(
            this Dataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, TRightOutput> selector)
        {
            return source.Select(item => item.Select(selector));
        }

        public static Dataflow<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightOutput>(
            this Dataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return source.SelectMany(item => item.SelectMany(selector));
        }

        public static Dataflow<Either<TLeft, TRightOutput>> SelectMany
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this Dataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => EitherExtensions.SelectMany(item, mediumSelector, resultSelector));
        }

        public static Dataflow<Either<TLeft, TRightOutput>> SelectSafe<TLeft, TRightInput, TRightOutput>(
            this Dataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Either<TLeft, TRightOutput>> selector)
        {
            return source.Select(item => item.SelectSafe(selector));
        }

        public static Dataflow<Either<TLeft, TRightOutput>> SelectSafe
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this Dataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, Either<TLeft, TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.Select(item => item.SelectSafe(mediumSelector, resultSelector));
        }

        public static Dataflow<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this Dataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> selector)
        {
            return source.SelectMany(item => item.SelectManySafe(selector));
        }

        public static Dataflow<Either<TLeft, TRightOutput>> SelectManySafe
            <TLeft, TRightInput, TRightMedium, TRightOutput>(
            this Dataflow<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightMedium>>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.SelectMany(item => item.SelectManySafe(mediumSelector, resultSelector));
        }

        //public static Dataflow<Either<TLeft, IGrouping<TKey, TRight>>> GroupBy<TLeft, TRight, TKey>(
        //    this Dataflow<Either<TLeft, TRight>> source, Func<TRight, TKey> keySelector)
        //{
        //    return source
        //        .GroupBy(item => item.IsRight)
        //        .SelectMany(
        //            group => group.Key
        //                ? group
        //                    .Rights()
        //                    .GroupBy(keySelector)
        //                    .Select(Prelude.Right<TLeft, IGrouping<TKey, TRight>>)
        //                : group
        //                    .Lefts()
        //                    .Select(Prelude.Left<TLeft, IGrouping<TKey, TRight>>));
        //}

        public static Dataflow<Either<TLeft, IList<TRight>>> BufferSafe<TLeft, TRight>(
            this Dataflow<Either<TLeft, TRight>> source, TimeSpan batchTimeout, int count)
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

        public static Dataflow<Either<TLeftOutput, TRightOutput>> Use<TInput, TLeftOutput, TRightOutput>(TInput disposable,
            Func<TInput, Dataflow<Either<TLeftOutput, TRightOutput>>> selector) where TInput : IDisposable
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