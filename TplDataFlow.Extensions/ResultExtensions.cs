using System;
using System.Collections.Generic;
using System.Linq;

namespace TplDataFlow.Extensions
{
    public static class ResultExtensions
    {
        public static Result<TSuccess, TFailure> ToResult<TSuccess, TFailure>(this TSuccess source)
        {
            return Result.Success<TSuccess, TFailure>(source);
        }

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

        public static IEnumerable<Result<TOutput, TFailure>> SelectMany<TInput, TOutput, TFailure>(
            this Result<TInput, TFailure> source, Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.Match(
                    success => selector(success).ToResult<TOutput, TFailure>(),
                    failure => Enumerable.Repeat(Result.Failure<TOutput, TFailure>(failure), 1));
        }

        public static IEnumerable<Result<TOutput, TFailure>> SelectManySafe<TInput, TOutput, TFailure>(
            this Result<TInput, TFailure> source, Func<TInput, IEnumerable<Result<TOutput, TFailure>>> selector)
        {
            return source.Match(selector, failure => Enumerable.Repeat(Result.Failure<TOutput, TFailure>(failure), 1));
        }
    }
}