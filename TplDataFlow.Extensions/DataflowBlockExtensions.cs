using System;
using System.Collections.Generic;
using System.Reactive.Linq;
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

        public static ISourceBlock<TOutput> Select<TInput, TOutput>(
            this ISourceBlock<TInput> source, Func<TInput, TOutput> selector)
        {
            return source.LinkWith(new TransformBlock<TInput, TOutput>(selector));
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

        public static ISourceBlock<Result<TOutput, TFailure>> SelectManySafe<TInput, TOutput, TFailure>(
            this ISourceBlock<Result<TInput, TFailure>> source, Func<TInput, IEnumerable<Result<TOutput, TFailure>>> selector)
        {
            return source.LinkWith(new TransformSafeBlock<TInput, TOutput, TFailure>(selector));
        }

        public static TResult Match<TInput, TFailure, TOutputSuccess, TOutputFailure, TResult>(
            this ISourceBlock<Result<TInput, TFailure>> source,
            Func<ISourceBlock<TInput>, TOutputSuccess> selectorOnSuccess,
            Func<ISourceBlock<TFailure>, TOutputFailure> selectorOnFailure,
            Func<TOutputSuccess, TOutputFailure, TResult> resultSelector)
        {
            var successBlock = new BufferBlock<Result<TInput, TFailure>>();
            var failureBlock = new BufferBlock<Result<TInput, TFailure>>();

            source
                .LinkWhen(result => result.IsSuccess, successBlock)
                .LinkOtherwise(failureBlock);

            return resultSelector(
                selectorOnSuccess(successBlock.Select(result => result.Success)),
                selectorOnFailure(failureBlock.Select(result => result.Failure)));
        }


        public static TResult Map<T, TOutputTrue, TOutputFalse, TResult>(this ISourceBlock<T> source, 
            Predicate<T> predicate,
            Func<ISourceBlock<T>, TOutputTrue> selectorOnTrue,
            Func<ISourceBlock<T>, TOutputFalse> selectorOnFalse,
            Func<TOutputTrue, TOutputFalse, TResult> resultSelector)
        {
            var trueBlock = new BufferBlock<T>();
            var falseBlock = new BufferBlock<T>();

            source
                .LinkWhen(predicate, trueBlock)
                .LinkOtherwise(falseBlock);

            return resultSelector(selectorOnTrue(trueBlock), selectorOnFalse(falseBlock));
        }

        public static ISourceBlock<Result<TSuccess, TFailure>> ToResult<TSuccess, TFailure>(this ISourceBlock<TSuccess> source)
        {
            return source.LinkWith(
                new TransformBlock<TSuccess, Result<TSuccess, TFailure>>(item =>
                    item.ToResult<TSuccess, TFailure>()));
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
            var outputBlock = new BufferBlock<Result<IList<TSuccess>, TFailure>>();

            source.AsObservable()
                .BufferSafe(batchTimeout, batchMaxSize)
                .Subscribe(outputBlock.AsObserver());

            return outputBlock;
        }
    }
}