using System;

namespace TplDataFlow.Extensions
{
    public static class ResultExtensions
    {
        public static Result<TSuccess, TFailure> ToResult<TSuccess, TFailure>(this TSuccess source)
        {
            return Result.Success<TSuccess, TFailure>(source);
        }

        public static void Match<TSuccess, TFailure>(this Result<TSuccess, TFailure> source,
            Action<TSuccess> onSuccess, Action<TFailure> onFailure)
        {
            if (source.IsSuccess)
            {
                onSuccess(source.Success);
            }
            else
            {
                onFailure(source.Failure);
            }
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
            if (!source.IsSuccess)
            {
                return Result.Failure<TOutput, TFailure>(source.Failure);
            }
            return Result.Success<TOutput, TFailure>(selector(source.Success));
        }

        public static Result<TOutput, TFailure> SelectSafe<TInput, TOutput, TFailure>(
            this Result<TInput, TFailure> source, Func<TInput, Result<TOutput, TFailure>> selector)
        {
            if (!source.IsSuccess)
            {
                return Result.Failure<TOutput, TFailure>(source.Failure);
            }
            return selector(source.Success);
        }
    }
}