using System;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public static class DataflowBlockExtensions
    {
        public static ISourceBlock<TOutput> LinkWith<TInput, TOutput>(this ISourceBlock<TInput> sourceBlock,
            IPropagatorBlock<TInput, TOutput> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true });
            return targetBlock;
        }

        public static ISourceBlock<T> LinkWhen<T>(this ISourceBlock<T> sourceBlock,
            Predicate<T> predicate,
            ITargetBlock<T> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true, Append = true },
                predicate);
            return sourceBlock;
        }

        public static void LinkOtherwise<T>(this ISourceBlock<T> sourceBlock,
            ITargetBlock<T> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true, Append = true });
        }
    }
}
