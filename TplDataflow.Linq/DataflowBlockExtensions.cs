﻿using System;
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
            throw new NotImplementedException();
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
    }
}