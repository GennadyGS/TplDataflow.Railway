using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using LanguageExt;
using TplDataFlow.Extensions.Railway.Linq;
using TplDataFlow.Extensions.TplDataflow.Extensions;

namespace TplDataFlow.Extensions.TplDataflow.Railway
{
    public static class DataflowBlockExtensions
    {
        public static ISourceBlock<Either<TLeft, TRightOutput>> SelectSafe<TLeft, TRightInput, TRightOutput>(
            this ISourceBlock<Either<TLeft, TRightInput>> source, Func<TRightInput, Either<TLeft, TRightOutput>> selector)
        {
            return source.LinkWith(new TransformSafeBlock<TLeft, TRightInput, TRightOutput>(selector));
        }

        public static ISourceBlock<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this ISourceBlock<Either<TLeft, TRightInput>> source, Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> selector)
        {
            return source.LinkWith(new TransformSafeBlock<TLeft, TRightInput, TRightOutput>(selector));
        }

        public static ISourceBlock<Either<TLeft, IList<TSuccess>>> BufferSafe<TLeft, TSuccess>(this ISourceBlock<Either<TLeft, TSuccess>> source,
            TimeSpan batchTimeout, int batchMaxSize)
        {
            var outputBlock = new BufferBlock<Either<TLeft, IList<TSuccess>>>();

            source.AsObservable()
                .BufferSafe(batchTimeout, batchMaxSize)
                .Subscribe(outputBlock.AsObserver());

            return outputBlock;
        }
    }
}