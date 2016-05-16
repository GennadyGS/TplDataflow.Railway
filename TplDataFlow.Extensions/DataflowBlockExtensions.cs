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

        public static void LinkOtherwise<T>(this ISourceBlock<T> sourceBlock,
            ITargetBlock<T> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true, Append = true });
        }

        public static ITargetBlock<TInput> LinkOtherwise<TInput, TMedium>(this IPropagatorBlock<TInput, TMedium> sourceBlock,
            ITargetBlock<TMedium> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true, Append = true });
            return sourceBlock;
        }

        public static void Split<T>(this ISourceBlock<T> source,
            Predicate<T> predicate, ITargetBlock<T> targetOnTrue, ITargetBlock<T> targetOnFalse)
        {
            source
                .LinkWhen(predicate, targetOnTrue)
                .LinkOtherwise(targetOnFalse);
        }

        public static ITargetBlock<TInput> Split<TInput, TOutput>(this IPropagatorBlock<TInput, TOutput> source,
            Predicate<TOutput> predicate, ITargetBlock<TOutput> targetOnTrue, ITargetBlock<TOutput> targetOnFalse)
        {
            return source
                .LinkWhen(predicate, targetOnTrue)
                .LinkOtherwise(targetOnFalse);
        }

        public static IPropagatorBlock<TInput, TOutput> CombineWith<TInput, TMedium, TOutput>(this IPropagatorBlock<TInput, TMedium> previous,
            IPropagatorBlock<TMedium, TOutput> next)
        {
            return new CombinedPropagatorBlock<TInput, TMedium, TOutput>(previous, next);
        }

        public static IPropagatorBlock<TInput, TOutput> Select<TInput, TMedium, TOutput>(
            this IPropagatorBlock<TInput, TMedium> self, Func<TMedium, TOutput> select)
        {
            return self.CombineWith(new TransformBlock<TMedium, TOutput>(select));
        }
    }
}