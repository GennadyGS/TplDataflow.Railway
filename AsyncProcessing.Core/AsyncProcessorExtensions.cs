using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AsyncProcessing.Core
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
    }
}