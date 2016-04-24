using System;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public static class DataflowBlockExtensions
    {
        public static void PropagateCompletion(this IDataflowBlock sourceBlock,
            params IDataflowBlock[] targetBlocks)
        {
            targetBlocks.ToList().ForEach(targetBlock =>
                sourceBlock.Completion.ContinueWith(task =>
                {
                    if (task.IsFaulted)
                        targetBlock.Fault(task.Exception);
                    else
                        targetBlock.Complete();
                }));
        }

        public static IDisposable LinkWith<T>(this ISourceBlock<T> sourceBlock,
            ITargetBlock<T> targetBlock)
        {
            return sourceBlock.LinkTo(targetBlock, 
                new DataflowLinkOptions { PropagateCompletion = true });
        }

        public static ITargetBlock<TInput> LinkWith<TInput, TMedium>(this IPropagatorBlock<TInput, TMedium> sourceBlock,
            ITargetBlock<TMedium> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock, 
                new DataflowLinkOptions { PropagateCompletion = true });
            return sourceBlock;
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

        public static IPropagatorBlock<TInput, TMedium> LinkWhen<TInput, TMedium>(this IPropagatorBlock<TInput, TMedium> sourceBlock, 
            Predicate<TMedium> predicate,
            ITargetBlock<TMedium> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true, Append = true },
                predicate);
            return sourceBlock;
        }

        public static IDisposable LinkOtherwise<T>(this ISourceBlock<T> sourceBlock,
            ITargetBlock<T> targetBlock)
        {
            return sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true, Append = true });
        }

        public static ITargetBlock<TInput> LinkOtherwise<TInput, TMedium>(this IPropagatorBlock<TInput, TMedium> sourceBlock,
            ITargetBlock<TMedium> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true, Append = true });
            return sourceBlock;
        }

        public static IPropagatorBlock<TInput, TOutput> Combine<TInput, TMedium, TOutput>(this IPropagatorBlock<TInput, TMedium> previous,
            IPropagatorBlock<TMedium, TOutput> next)
        {
            return new CombinedPropagatorBlock<TInput, TMedium, TOutput>(previous, next);
        }
    }
}