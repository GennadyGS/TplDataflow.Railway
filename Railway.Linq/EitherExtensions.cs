using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Railway.Linq
{
    public static class EitherExtensions
    {
        public static TRight GetRightSafe<TLeft, TRight>(this Either<TLeft, TRight> input)
        {
            return input.IfLeft(() => failwith<TRight>("Not in right state"));
        }

        public static TLeft GetLeftSafe<TLeft, TRight>(this Either<TLeft, TRight> input)
        {
            return input.IfRight(() => failwith<TLeft>("Not in left state"));
        }

        public static async Task<Either<TLeft, TRightOutput>> SelectAsync<TLeft, TRightInput, TRightOutput>(
            this Task<Either<TLeft, TRightInput>> source, 
            Func<TRightInput, TRightOutput> selector)
        {
            return (await source).Select(selector);
        }

        public static Either<TLeft, TRightOutput> SelectSafe<TLeft, TRightInput, TRightOutput>(
            this Either<TLeft, TRightInput> source, Func<TRightInput, Either<TLeft, TRightOutput>> selector)
        {
            return source.Bind(selector);
        }

        public static Either<TLeft, TRightOutput> SelectSafe<TLeft, TRightInput, TRightMedium, TRightOutput>(
            this Either<TLeft, TRightInput> source,
            Func<TRightInput, Either<TLeft, TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.Bind(rightInput => 
                    mediumSelector(rightInput)
                        .Map(rightMedium => resultSelector(rightInput, rightMedium)));
        }

        public static Task<Either<TLeft, TRightOutput>> SelectSafeAsync<TLeft, TRightInput, TRightOutput>(
            this Either<TLeft, TRightInput> source, Func<TRightInput, Task<Either<TLeft, TRightOutput>>> selector)
        {
            return source.Match(
                async right => await selector(right), 
                left => Left<TLeft, TRightOutput>(left).AsTask());
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightOutput>(
            this Either<TLeft, TRightInput> source, Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return source.Match(
                right => selector(right).Select(Right<TLeft, TRightOutput>),
                left => List(Left<TLeft, TRightOutput>(left)));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightMedium, TRightOutput>(
            this Either<TLeft, TRightInput> source,
            Func<TRightInput, IEnumerable<TRightMedium>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.Match(
                right => mediumSelector(right)
                    .Select(medium => resultSelector(right, medium))
                    .Select(Right<TLeft, TRightOutput>),
                left => List(Left<TLeft, TRightOutput>(left)));
        }

        public static async Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectManyAsync<TLeft, TRightInput, TRightOutput>(
            this Task<Either<TLeft, TRightInput>> source, Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return (await source).SelectMany(selector);
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this Either<TLeft, TRightInput> source, 
            Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> selector)
        {
            return source.Match(
                selector,
                left => List(Left<TLeft, TRightOutput>(left)));
        }

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightMedium, TRightOutput>(
            this Either<TLeft, TRightInput> source,
            Func<TRightInput, IEnumerable<Either<TLeft, TRightMedium>>> mediumSelector,
            Func<TRightInput, TRightMedium, TRightOutput> resultSelector)
        {
            return source.Match(
                right => mediumSelector(right).Select(medium => resultSelector(right, medium)),
                left => List(Left<TLeft, TRightOutput>(left)));
        }

        public static async Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectManySafeAsync<TLeft, TRightInput, TRightOutput>(
            this Either<TLeft, TRightInput> source,
            Func<TRightInput, Task<IEnumerable<Either<TLeft, TRightOutput>>>> selector)
        {
            return await source.Match(
                selector,
                left => Task.FromResult(List(Left<TLeft, TRightOutput>(left)).AsEnumerable()));
        }

        public static async Task<IEnumerable<Either<TLeft, TRightOutput>>> SelectManySafeAsync<TLeft, TRightInput, TRightOutput>(
            this Task<Either<TLeft, TRightInput>> source, 
            Func<TRightInput, Task<IEnumerable<Either<TLeft, TRightOutput>>>> selector)
        {
            return await (await source).SelectManySafeAsync(selector);
        }
    }
}