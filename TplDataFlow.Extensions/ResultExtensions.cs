using System;
using System.Collections.Generic;

namespace TplDataFlow.Extensions
{
    public static class ResultExtensions
    {
        public static T Match<TSuccess, TFailure, T>(this Result<TSuccess, TFailure> source, Func<TSuccess, T> onSuccess, Func<TFailure, T> onFailure)
        {
            return source.IsSuccess
                ? onSuccess(source.Success)
                : onFailure(source.Failure);
        }

        public static Result<TOutput, TFailure> Select<TInput, TOutput, TFailure>(this Result<TInput, TFailure> source, Func<TInput, TOutput> selector)
        {
            if (!source.IsSuccess)
            {
                return Result.Failure<TOutput, TFailure>(source.Failure);
            }
            return Result.Success<TOutput, TFailure>(selector(source.Success));
        }

        public static Result<TOutput, TFailure> Select<TInput, TOutput, TFailure>(this Result<TInput, TFailure> source, Func<TInput, Result<TOutput, TFailure>> selector)
        {
            if (!source.IsSuccess)
            {
                return Result.Failure<TOutput, TFailure>(source.Failure);
            }
            return selector(source.Success);
        }

        public static Result<IEnumerable<TOutput>, TFailure> SelectMany<TInput, TOutput, TFailure>(this Result<TInput, TFailure> source, Func<TInput, Result<IEnumerable<TOutput>, TFailure>> selector)
        {
            if (!source.IsSuccess)
            {
                return Result.Failure<IEnumerable<TOutput>, TFailure>(source.Failure);
            }
            return selector(source.Success);
        }

        public static Result<TSuccess, TFailure> ToResult<TSuccess, TFailure>(this TSuccess source)
        {
            return Result.Success<TSuccess, TFailure>(source);
        }
    }
}