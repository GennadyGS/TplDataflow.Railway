using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using TplDataFlow.Extensions;

namespace TplDataflow.Linq
{
    public static class DataflowBlockExtensions
    {
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

        public static ISourceBlock<TOutput> SelectMany<TInput, TOutput>(
            this ISourceBlock<TInput> source, Func<TInput, ISourceBlock<TOutput>> selector)
        {
            throw new NotImplementedException();
        }

        public static ISourceBlock<GroupedSourceBlock<TKey, TElement>> GroupBy<TElement, TKey>(
            this ISourceBlock<TElement> source, Func<TElement, TKey> keySelector)
        {
            return source.LinkWith(CreateGroupByBlock(keySelector));
        }

        public static ISourceBlock<TOutput> Cast<TInput, TOutput>(this ISourceBlock<TInput> source)
        {
            throw new NotImplementedException();
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

        private static IPropagatorBlock<TElement, GroupedSourceBlock<TKey, TElement>> CreateGroupByBlock<TKey, TElement>(
            Func<TElement, TKey> keySelector)
        {
            var groups = new ConcurrentDictionary<TKey, GroupedSourceBlock<TKey, TElement>>();
            var sourceBlock = new BufferBlock<GroupedSourceBlock<TKey, TElement>>();
            var targetBlock = new ActionBlock<TElement>(item =>
            {
                var groupedSourceBlock = groups.AddOrUpdate(keySelector(item),
                    key =>
                    {
                        var result = new GroupedSourceBlock<TKey, TElement>(key);
                        sourceBlock.Post(result);
                        return result;
                    }, 
                    (key, block) => block);
                groupedSourceBlock.Post(item);
            });
            targetBlock.PropagateCompletionTo(sourceBlock);
        return DataflowBlock.Encapsulate(targetBlock, sourceBlock);
        }
    }
}