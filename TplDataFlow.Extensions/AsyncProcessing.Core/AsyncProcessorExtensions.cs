using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace TplDataFlow.Extensions.AsyncProcessing.Core
{
    public static class AsyncProcessorExtensions
    {
        //TODO: Refactor
        public static IList<TOutput> InvokeSync<TInput, TOutput>(this IAsyncProcessor<TInput, TOutput> processor, IList<TInput> input)
        {
            var result = processor.SubscribeList();
            input.ToObservable().Subscribe(processor);
            processor.LastOrDefaultAsync().GetAwaiter().GetResult();
            return result;
        }

        private static IObserver<T> CreateObserver<T>(this ICollection<T> collection)
        {
            return Observer.Create<T>(collection.Add);
        }

        private static IList<T> SubscribeList<T>(this IObservable<T> observable)
        {
            var result = new List<T>();
            observable.Subscribe(result.CreateObserver());
            return result;
        }
    }
}