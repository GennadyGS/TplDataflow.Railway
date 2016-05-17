using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

        public static ISourceBlock<TOutput> LinkWith<TInput, TOutput>(this ISourceBlock<TInput> sourceBlock,
            IPropagatorBlock<TInput, TOutput> targetBlock)
        {
            sourceBlock.LinkTo(targetBlock,
                new DataflowLinkOptions { PropagateCompletion = true });
            return targetBlock;
        }

        public static ITargetBlock<TInput> LinkWith<TInput, TOutput>(this IPropagatorBlock<TInput, TOutput> sourceBlock,
            ITargetBlock<TOutput> targetBlock)
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

        public static IPropagatorBlock<TInput, TOutput> LinkWhen<TInput, TOutput>(this IPropagatorBlock<TInput, TOutput> sourceBlock,
            Predicate<TOutput> predicate,
            ITargetBlock<TOutput> targetBlock)
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

        public static IPropagatorBlock<TInput, TOutput> CombineWith<TInput, TMedium, TOutput>(this IPropagatorBlock<TInput, TMedium> previous,
            IPropagatorBlock<TMedium, TOutput> next)
        {
            return new CombinedPropagatorBlock<TInput, TMedium, TOutput>(previous, next);
        }

        public static ISourceBlock<TOutput> Select<TInput, TOutput>(
            this ISourceBlock<TInput> source, Func<TInput, TOutput> selector)
        {
            return source.LinkWith(new TransformBlock<TInput,TOutput>(selector));
        }

        public static ISourceBlock<TOutput> SelectMany<TInput, TOutput>(
            this ISourceBlock<TInput> source, Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.LinkWith(new TransformManyBlock<TInput, TOutput>(selector));
        }

        public static ISourceBlock<Result<TOutput, TFailure>> SelectSafe<TInput, TOutput, TFailure>(
            this ISourceBlock<Result<TInput, TFailure>> source, Func<TInput, Result<TOutput, TFailure>> selector)
        {
            return source.LinkWith(new TransformSafeBlock<TInput, TOutput, TFailure>(selector));
        }

        // TODO: Refactoring
        public static ISourceBlock<Result<TOutput, TFailure>> SelectManySafe<TInput, TOutput, TFailure>(
            this ISourceBlock<Result<TInput, TFailure>> source, Func<TInput, Result<IEnumerable<TOutput>, TFailure>> selector)
        {
            return source.LinkWith(
                new TransformManyBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>>(
                    item => item.SelectManySafeFromResult(selector)));
        }

        public static void Match<TSuccess, TFailure>(this ISourceBlock<Result<TSuccess, TFailure>> source,
            Action<ISourceBlock<TSuccess>> onSuccess, Action<ISourceBlock<TFailure>> onFailure)
        {
            var successBlock = new BufferBlock<Result<TSuccess, TFailure>>();
            var failureBlock = new BufferBlock<Result<TSuccess, TFailure>>();

            source
                .LinkWhen(result => result.IsSuccess, successBlock)
                .LinkOtherwise(failureBlock);

            onSuccess(successBlock.Select(result => result.Success));
            onFailure(failureBlock.Select(result => result.Failure));
        }

        public static void Split<T>(this ISourceBlock<T> source,
            Predicate<T> predicate, Action<ISourceBlock<T>> onTrue, Action<ISourceBlock<T>> onFalse)
        {
            var trueBlock = new BufferBlock<T>();
            var falseBlock = new BufferBlock<T>();

            source
                .LinkWhen(predicate, trueBlock)
                .LinkOtherwise(falseBlock);

            onTrue(trueBlock);
            onFalse(falseBlock);
        }


        public static ISourceBlock<Result<TSuccess, TFailure>> ToResult<TSuccess, TFailure>(this ISourceBlock<TSuccess> source)
        {
            return source.LinkWith(
                new TransformBlock<TSuccess, Result<TSuccess, TFailure>>(item =>
                    item.ToResult<TSuccess, TFailure>()));
        }

        public static ISourceBlock<T> SideEffect<T>(this ISourceBlock<T> source, Action<T> sideEffect)
        {
            return source.LinkWith(new TransformBlock<T, T>(item =>
                {
                    sideEffect(item);
                    return item;
                }));
        }

        public static ISourceBlock<IList<T>> Buffer<T>(this ISourceBlock<T> source,
            TimeSpan batchTimeout, int batchMaxSize)
        {
            var outputBlock = new BufferBlock<IList<T>>();

            source.AsObservable()
                .Buffer(batchTimeout, batchMaxSize)
                .Where(buffer => buffer.Count > 0)
                .Subscribe(outputBlock.AsObserver());

            return outputBlock;
        }

        public static ISourceBlock<Result<IList<TSuccess>, TFailure>> BufferSafe<TSuccess, TFailure>(this ISourceBlock<Result<TSuccess, TFailure>> source,
            TimeSpan batchTimeout, int batchMaxSize)
        {
            return source.LinkWith(
                DataflowBlockFactory.CreateTimedBatchBlockSafe<TSuccess, TFailure>(batchTimeout, batchMaxSize));
        }

        // TODO: Get rid of it
        private static IEnumerable<Result<TOutput, TFailure>> SelectManySafeFromResult<TInput, TOutput, TFailure>(this Result<TInput, TFailure> source,
            Func<TInput, Result<IEnumerable<TOutput>, TFailure>> selector)
        {
            if (!source.IsSuccess)
            {
                return Enumerable.Repeat(Result.Failure<TOutput, TFailure>(source.Failure), 1);
            }
            Result<IEnumerable<TOutput>, TFailure> res = selector(source.Success);
            return res.Match(success => success.Select<TOutput, Result<TOutput, TFailure>>(item => item.ToResult<TOutput, TFailure>()),
                failure => Enumerable.Repeat(Result.Failure<TOutput, TFailure>(failure), 1));
        }

    }
}