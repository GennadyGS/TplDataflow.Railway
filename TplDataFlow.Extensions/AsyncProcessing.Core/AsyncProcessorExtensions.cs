using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace TplDataFlow.Extensions.AsyncProcessing.Core
{
    public static class AsyncProcessorExtensions
    {
        public static IList<TOutput> InvokeSync<TInput, TOutput>(this IAsyncProcessor<TInput, TOutput> processor, IList<TInput> input)
        {
            return InvokeAsync(processor, input).Result;
        }

        private static Task<IList<TOutput>> InvokeAsync<TInput, TOutput>(IAsyncProcessor<TInput, TOutput> processor, IList<TInput> input)
        {
            var result = processor.ToListAsync();
            input.ToObservable().Subscribe(processor);
            return result;
        }

        private static Task<IList<T>> ToListAsync<T>(this IObservable<T> observable)
        {
            var taskSource = new TaskCompletionSource<IList<T>>();
            var result = new List<T>();
            observable.Subscribe(
                onNext: result.Add,
                onError: taskSource.SetException,
                onCompleted: () => taskSource.SetResult(result));
            return taskSource.Task;
        }
    }
}