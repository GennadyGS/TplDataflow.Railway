using System;

namespace TplDataFlow.Extensions
{
    public static class ResultExtensions
    {
        public static T Match<TSuccess, TFailure, T>(this Result<TSuccess, TFailure> self, Func<TSuccess, T> onSuccess, Func<TFailure, T> onFailure)
        {
            return self.IsSuccess
                ? onSuccess(self.Success)
                : onFailure(self.Failure);
        }

        public static Result<TOutput, TFailure> Select<TInput, TOutput, TFailure>(this Result<TInput, TFailure> self, Func<TInput, TOutput> selector)
        {
            if (!self.IsSuccess)
            {
                return Result.Failure<TOutput, TFailure>(self.Failure);
            }
            return Result.Success<TOutput, TFailure>(selector(self.Success));
        }

        public static Result<TOutput, TFailure> Select<TInput, TOutput, TFailure>(this Result<TInput, TFailure> self, Func<TInput, Result<TOutput, TFailure>> selector)
        {
            if (!self.IsSuccess)
            {
                return Result.Failure<TOutput, TFailure>(self.Failure);
            }
            return selector(self.Success);
        }
    }
}