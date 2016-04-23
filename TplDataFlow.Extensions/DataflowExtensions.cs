using System;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public static class DataflowExtensions
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
            return sourceBlock.LinkTo(targetBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        public static IPropagatorBlock<TInput, TOutput> Combine<TInput, TMedium, TOutput>(this IPropagatorBlock<TInput, TMedium> previous,
            IPropagatorBlock<TMedium, TOutput> next)
        {
            return new CombinedPropagatorBlock<TInput, TMedium, TOutput>(previous, next);
        }
    }
}