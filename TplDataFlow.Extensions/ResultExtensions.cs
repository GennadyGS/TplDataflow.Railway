using System;
using System.Collections.Generic;
using System.Linq;

namespace TplDataFlow.Extensions
{
    public static class ResultExtensions
    {
        public static TOutput Match<TInput, TOutput, TFailure>(this Result<TInput, TFailure> source,
            Func<TInput, TOutput> onSuccess, Func<TFailure, TOutput> onFailure)
        {
            return source.IsSuccess
                ? onSuccess(source.Success)
                : onFailure(source.Failure);
        }

        public static Result<TOutput, TFailure> Select<TInput, TOutput, TFailure>(this Result<TInput, TFailure> source,
            Func<TInput, TOutput> selector)
        {
            return source.Match(success => selector(success), Result.Failure<TOutput, TFailure>);
        }

        public static Result<TOutput, TFailure> SelectSafe<TInput, TOutput, TFailure>(
            this Result<TInput, TFailure> source, Func<TInput, Result<TOutput, TFailure>> selector)
        {
            return source.Match(selector, Result.Failure<TOutput, TFailure>);
        }

        public static Result<TOutput, TFailure> SelectSafe<TInput, TMedium, TOutput, TFailure>(
            this Result<TInput, TFailure> source, 
            Func<TInput, Result<TMedium, TFailure>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.Match(
                success => mediumSelector(success).Match(
                    mediumSuccess => resultSelector(success, mediumSuccess), 
                    Result.Failure<TOutput, TFailure>),
                Result.Failure<TOutput, TFailure>);
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectMany<TInput, TOutput, TFailure>(
            this Result<TInput, TFailure> source, Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.Match(
                    success => selector(success).Select(Result.Success<TOutput, TFailure>),
                    failure => Enumerable.Repeat(Result.Failure<TOutput, TFailure>(failure), 1));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectMany<TInput, TOutput, TMedium, TFailure>(
            this Result<TInput, TFailure> source, 
            Func<TInput, IEnumerable<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.Match(
                    success => mediumSelector(success)
                        .Select(medium => resultSelector(success, medium))
                        .Select(Result.Success<TOutput, TFailure>),
                    failure => Enumerable.Repeat(Result.Failure<TOutput, TFailure>(failure), 1));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectManySafe<TInput, TOutput, TFailure>(
            this Result<TInput, TFailure> source, Func<TInput, IEnumerable<Result<TOutput, TFailure>>> selector)
        {
            return source.Match(selector, 
                failure => Enumerable.Repeat(Result.Failure<TOutput, TFailure>(failure), 1));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectManySafe<TInput, TMedium, TOutput, TFailure>(
            this Result<TInput, TFailure> source, 
            Func<TInput, IEnumerable<Result<TMedium, TFailure>>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.Match(
                success => mediumSelector(success)
                    .Select(medium => resultSelector(success, medium)),
                failure => Enumerable.Repeat(Result.Failure<TOutput, TFailure>(failure), 1));
        }
    }
}