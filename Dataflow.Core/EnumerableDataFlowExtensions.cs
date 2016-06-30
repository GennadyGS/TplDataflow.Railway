using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public static class EnumerableDataFlowExtensions
    {
        public static IEnumerable<TOutput> BindDataflow<TInput, TOutput>(this IEnumerable<TInput> input, 
            Func<TInput, Dataflow<TOutput>> bindFunc)
        {
            if (!input.Any())
            {
                return Enumerable.Empty<TOutput>();
            }
            var dataflows = input.Select(bindFunc);
            return dataflows.First().TransformEnumerableOfDataFlow(dataflows);
        }
    }
}