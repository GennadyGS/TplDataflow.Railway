using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using TplDataFlow.Extensions.Railway.Core;
using TplDataFlow.Extensions.Railway.Linq;
using TplDataFlow.Extensions.TplDataflow.Extensions;
using TplDataFlow.Extensions.TplDataflow.Linq;

namespace TplDataFlow.Extensions.TplDataflow.Railway
{
    public static class DataflowBlockExtensions
    {
        public static ISourceBlock<Result<TOutput, TFailure>> SelectSafe<TInput, TOutput, TFailure>(
            this ISourceBlock<Result<TInput, TFailure>> source, Func<TInput, Result<TOutput, TFailure>> selector)
        {
            return source.LinkWith(new TransformSafeBlock<TInput, TOutput, TFailure>(selector));
        }

        public static ISourceBlock<Result<TOutput, TFailure>> SelectManySafe<TInput, TOutput, TFailure>(
            this ISourceBlock<Result<TInput, TFailure>> source, Func<TInput, IEnumerable<Result<TOutput, TFailure>>> selector)
        {
            return source.LinkWith(new TransformSafeBlock<TInput, TOutput, TFailure>(selector));
        }

        public static TResult Match<TInput, TInputFailure, TOutput, TOutputFailure, TResult>(
            this ISourceBlock<Result<TInput, TInputFailure>> source,
            Func<ISourceBlock<TInput>, TOutput> selectorOnSuccess,
            Func<ISourceBlock<TInputFailure>, TOutputFailure> selectorOnFailure,
            Func<TOutput, TOutputFailure, TResult> resultSelector)
        {
            var successBlock = new BufferBlock<Result<TInput, TInputFailure>>();
            var failureBlock = new BufferBlock<Result<TInput, TInputFailure>>();

            source
                .LinkWhen(result => result.IsSuccess, successBlock)
                .LinkOtherwise(failureBlock);

            return resultSelector(
                selectorOnSuccess(successBlock.Select(result => result.Success)),
                selectorOnFailure(failureBlock.Select(result => result.Failure)));
        }

        public static ISourceBlock<Result<IList<TSuccess>, TFailure>> BufferSafe<TSuccess, TFailure>(this ISourceBlock<Result<TSuccess, TFailure>> source,
            TimeSpan batchTimeout, int batchMaxSize)
        {
            var outputBlock = new BufferBlock<Result<IList<TSuccess>, TFailure>>();

            source.AsObservable()
                .BufferSafe(batchTimeout, batchMaxSize)
                .Subscribe(outputBlock.AsObserver());

            return outputBlock;
        }
    }
}