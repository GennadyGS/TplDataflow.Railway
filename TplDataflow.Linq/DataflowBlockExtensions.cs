using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TplDataFlow.Extensions;

namespace TplDataflow.Linq
{
    public static class DataflowBlockExtensions
    {
        public static ISourceBlock<T> Concat<T>(this ISourceBlock<ISourceBlock<T>> source)
        {
            return source.LinkWith(CreateConcatBlock(source));
        }

        public static ISourceBlock<TOutput> Select<TInput, TOutput>(
            this ISourceBlock<TInput> source, Func<TInput, TOutput> selector)
        {
            return source.LinkWith(new TransformBlock<TInput, TOutput>(selector));
        }

		public static ISourceBlock<TOutput> SelectAsync<TInput, TOutput>(
			this ISourceBlock<TInput> source, Func<TInput, Task<TOutput>> selector)
		{
			return source.LinkWith(new TransformBlock<TInput, TOutput>(selector));
		}

		public static ISourceBlock<TOutput> SelectMany<TInput, TOutput>(
            this ISourceBlock<TInput> source, Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.LinkWith(new TransformManyBlock<TInput, TOutput>(selector));
        }

        public static ISourceBlock<TOutput> SelectManyAsync<TInput, TOutput>(
            this ISourceBlock<TInput> source, Func<TInput, Task<IEnumerable<TOutput>>> selector)
        {
            return source.LinkWith(new TransformManyBlock<TInput, TOutput>(selector));
        }

        public static ISourceBlock<TOutput> SelectMany<TInput, TOutput>(
            this ISourceBlock<TInput> source, Func<TInput, ISourceBlock<TOutput>> selector)
        {
            return source.Select(selector).Concat();
        }

        public static ISourceBlock<T> Where<T>(this ISourceBlock<T> source, Predicate<T> predicate)
        {
            var buffer = new BufferBlock<T>();
            source.LinkTo(buffer, new DataflowLinkOptions { PropagateCompletion = true }, predicate);
            return buffer;
        }

        public static ISourceBlock<GroupedSourceBlock<TKey, TElement>> GroupBy<TElement, TKey>(
            this ISourceBlock<TElement> source, Func<TElement, TKey> keySelector)
        {
            return source.LinkWith(CreateGroupByBlock(keySelector));
        }

        public static ISourceBlock<TResult> Cast<TResult>(this ISourceBlock<object> source)
        {
            return source.Select(item => (TResult)item);
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

        public static ISourceBlock<IList<T>> ToList<T>(this ISourceBlock<T> source)
        {
            return source.LinkWith(CreateToListBlock<T>());
        }

        private static IPropagatorBlock<ISourceBlock<T>, T> CreateConcatBlock<T>(ISourceBlock<ISourceBlock<T>> source)
        {
            JointPointBlock<T> sourceBlock = new JointPointBlock<T>();
            ITargetBlock<ISourceBlock<T>> targetBlock = new ActionBlock<ISourceBlock<T>>(block =>
            {
                block.LinkTo(sourceBlock.AddInput(), new DataflowLinkOptions { PropagateCompletion = true });
            });
            targetBlock.PropagateCompletionTo(sourceBlock.AddInput());
            return DataflowBlock.Encapsulate(targetBlock, sourceBlock);
        }

        private static IPropagatorBlock<TElement, GroupedSourceBlock<TKey, TElement>> CreateGroupByBlock<TKey, TElement>(
            Func<TElement, TKey> keySelector)
        {
            var groups = new ConcurrentDictionary<TKey, GroupedSourceBlock<TKey, TElement>>();
            var sourceBlock = new BufferBlock<GroupedSourceBlock<TKey, TElement>>();
            var targetBlock = new ActionBlock<TElement>(item =>
            {
                var groupedSourceBlock = groups.GetOrAdd(keySelector(item),
                    key =>
                    {
                        var result = new GroupedSourceBlock<TKey, TElement>(key);
                        sourceBlock.Post(result);
                        return result;
                    });
                groupedSourceBlock.Post(item);
            });
            targetBlock.Completion.ContinueWith(
                task =>
                {
                    groups.Values
                        .ToList()
                        .ForEach(group => group.SetCompletionFromTask(task));
                    sourceBlock.SetCompletionFromTask(task);
                });
            return DataflowBlock.Encapsulate(targetBlock, sourceBlock);
        }

        private static IPropagatorBlock<T, IList<T>> CreateToListBlock<T>()
        {
            var list = new List<T>();
            var sourceBlock = new BufferBlock<IList<T>>();
            var targetBlock = new ActionBlock<T>(item =>
            {
                list.Add(item);
            });
            targetBlock.Completion.ContinueWith(
                task =>
                {
                    sourceBlock.Post(list);
                    sourceBlock.SetCompletionFromTask(task);
                });
            return DataflowBlock.Encapsulate(targetBlock, sourceBlock);
        }
    }
}