using System.Collections.Generic;
using System.Reactive.Linq;

namespace TplDataFlow.Extensions.AsyncProcessing.Core
{
    public static class AsyncProcessorExtensions
    {
        public static IList<TOutput> InvokeSync<TInput, TOutput>(this IAsyncProcessor<TInput, TOutput> processor, IList<TInput> input)
        {
            input.ToObservable().Subscribe(processor);
            return processor.ToList().GetAwaiter().GetResult();
        }
    }
}