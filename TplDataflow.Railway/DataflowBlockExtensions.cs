using LanguageExt;
using Railway.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TplDataflow.Linq;
using TplDataFlow.Extensions;
using static LanguageExt.Prelude;

namespace TplDataflow.Railway
{
    public static class DataflowBlockExtensions
    {
        public static ISourceBlock<Either<TLeft, TRightOutput>> SelectMany<TLeft, TRightInput, TRightOutput>(
            this ISourceBlock<Either<TLeft, TRightInput>> source,
            Func<TRightInput, IEnumerable<TRightOutput>> selector)
        {
            return source.SelectMany(item => item.SelectMany(selector));
        }

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

        public static ISourceBlock<Either<TLeft, TRightOutput>> SelectManySafe<TLeft, TRightInput, TRightOutput>(
            this ISourceBlock<Either<TLeft, TRightInput>> source, Func<TRightInput, ISourceBlock<Either<TLeft, TRightOutput>>> selector)
        {
            return source
                .GroupBy(item => item.IsRight)
                .SelectMany(group => group.Key
                    ? group.SelectMany(item => selector(item.GetRightSafe()))
                    : group.Select(item => Left<TLeft, TRightOutput>(item.GetLeftSafe())));
        }

        public static ISourceBlock<Either<TLeft, TRightOutput>> SelectManySafeAsync<TLeft, TRightInput, TRightOutput>(
            this ISourceBlock<Either<TLeft, TRightInput>> source, 
            Func<TRightInput, Task<IEnumerable<Either<TLeft, TRightOutput>>>> selector)
        {
            return source
                .GroupBy(item => item.IsRight)
                .SelectMany(group => group.Key
                    ? group.SelectManyAsync(item => selector(item.GetRightSafe()))
                    : group.Select(item => Left<TLeft, TRightOutput>(item.GetLeftSafe())));
        }

        public static ISourceBlock<Either<TLeft, GroupedSourceBlock<TKey, TRight>>> GroupBySafe<TLeft, TRight, TKey>(
            this ISourceBlock<Either<TLeft, TRight>> source, Func<TRight, TKey> keySelector)
        {
            return source
                .GroupBy(item => item.IsRight)
                .SelectMany(
                    group => group.Key
                        ? group
                            .SelectMany(item => item.RightAsEnumerable())
                            .GroupBy(keySelector)
                            .Select(Right<TLeft, GroupedSourceBlock<TKey, TRight>>)
                        : group
                            .SelectMany(item => item.LeftAsEnumerable())
                            .Select(Left<TLeft, GroupedSourceBlock<TKey, TRight>>));
        }

        public static ISourceBlock<Either<TLeft, IList<TSuccess>>> BufferSafe<TLeft, TSuccess>(this ISourceBlock<Either<TLeft, TSuccess>> source,
                int batchMaxSize)
        {
            var outputBlock = new BufferBlock<Either<TLeft, IList<TSuccess>>>();

            source.AsObservable()
                .BufferSafe(batchMaxSize)
                .Subscribe(outputBlock.AsObserver());

            return outputBlock;
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

        public static ISourceBlock<Either<TLeftOutput, TRightOutput>> Use<TInput, TLeftOutput, TRightOutput>(TInput disposable,
            Func<TInput, ISourceBlock<Either<TLeftOutput, TRightOutput>>> selector) where TInput : IDisposable
        {
            return selector(disposable)
                .Select(item =>
                {
                    disposable.Dispose();
                    return item;
                });
        }
    }
}