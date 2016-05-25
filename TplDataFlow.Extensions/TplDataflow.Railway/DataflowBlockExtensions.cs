using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using TplDataFlow.Extensions.Railway.Core;
using TplDataFlow.Extensions.Railway.Linq;
using TplDataFlow.Extensions.TplDataflow.Extensions;

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