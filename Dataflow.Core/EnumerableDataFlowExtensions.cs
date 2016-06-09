using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public static class EnumerableDataFlowExtensions
    {
        public static IEnumerable<TOutput> BindDataflow<TInput, TOutput>(this IEnumerable<TInput> input, Func<TInput, Dataflow<TOutput>> bindFunc)
        {
            return TransformDataflow(input.Select(bindFunc));
        }

        private static IEnumerable<TOutput> TransformDataflow<TOutput>(IEnumerable<Dataflow<TOutput>> dataflows)
        {
            return dataflows
                .GroupBy(dataflow => dataflow.IsReturn)
                .SelectMany(group => group.Key
                    ? group.Select(dataflow => ((Return<TOutput>) dataflow).Result)
                    : TransformDataflow(group.Select(dataflow => ((Continuation<TOutput>) dataflow).Func())));
        }
    }
}