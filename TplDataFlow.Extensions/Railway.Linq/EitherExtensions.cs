﻿using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using static LanguageExt.Prelude;

namespace TplDataFlow.Extensions.Railway.Linq
{
    public static class EitherExtensions
    {
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

        public static IEnumerable<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this Either<TLeft, TRightInput> source, Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> selector)
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
    }
}