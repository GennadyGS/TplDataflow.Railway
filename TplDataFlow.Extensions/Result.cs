using System;

namespace TplDataFlow.Extensions
{
    public class Result<TSuccess, TFailure>
    {
        private readonly TSuccess _success;
        private readonly TFailure _failure;

        internal Result(TSuccess success)
        {
            _success = success;
            _failure = default(TFailure);
            IsSuccess = true;
        }

        internal Result(TFailure failure)
        {
            _success = default(TSuccess);
            _failure = failure;
            IsSuccess = false;
        }

        public TSuccess Success
        {
            get
            {
                if (!IsSuccess)
                {
                    throw new InvalidOperationException("Result is not success");
                }
                return _success;
            }
        }

        public TFailure Failure
        {
            get
            {
                if (IsSuccess)
                {
                    throw new InvalidOperationException("Result is not failure");
                }
                return _failure;
            }
        }

        public bool IsSuccess { get; }

        public static implicit operator Result<TSuccess, TFailure>(TSuccess success)
        {
            return new Result<TSuccess, TFailure>(success);
        }

        public static implicit operator Result<TSuccess, TFailure>(TFailure failure)
        {
            return new Result<TSuccess, TFailure>(failure);
        }
    }

    public static class Result
    {
        public static Result<TSuccess, TFailure> Success<TSuccess, TFailure>(TSuccess success)
        {
            return new Result<TSuccess, TFailure>(success);
        }

        public static Result<TSuccess, TFailure> Failure<TSuccess, TFailure>(TFailure failure)
        {
            return new Result<TSuccess, TFailure>(failure);
        }

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
                return Failure<TOutput, TFailure>(self.Failure);
            }
            return Success<TOutput, TFailure>(selector(self.Success));
        }

        public static Result<TOutput, TFailure> Select<TInput, TOutput, TFailure>(this Result<TInput, TFailure> self, Func<TInput, Result<TOutput, TFailure>> selector)
        {
            if (!self.IsSuccess)
            {
                return Failure<TOutput, TFailure>(self.Failure);
            }
            return selector(self.Success);
        }

        public static Result<TOutput, TFailure> SelectMany<TInput, TOutput, TFailure>(this Result<TInput, TFailure> self, Func<TInput, Result<TOutput, TFailure>> selector)
        {
            if (!self.IsSuccess)
            {
                return Failure<TOutput, TFailure>(self.Failure);
            }
            return selector(self.Success);
        }
    }
}