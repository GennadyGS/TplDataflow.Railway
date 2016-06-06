using System;

namespace EventProcessing.Utils
{
    public static class ObjectExtensions
    {
        public static TOutput Apply<TInput, TOutput>(this TInput input, Func<TInput, TOutput> func)
        {
            return func(input);
        }
    }
}