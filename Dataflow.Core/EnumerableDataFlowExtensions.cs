using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public static class EnumerableDataFlowExtensions
    {
        public static IEnumerable<TOutput> BindDataflow<TInput, TOutput>(this IEnumerable<TInput> input, Func<TInput, Dataflow<TOutput>> bindFunc)
        {
            var enumerable = input.Select(bindFunc);
            return TransformDataflow(enumerable);
        }

        private static IEnumerable<TOutput> TransformDataflow<TOutput>(IEnumerable<Dataflow<TOutput>> dataflows)
        {
            return dataflows
                .GroupBy(dataflow => dataflow.IsReturn)
                .SelectMany(group => group.Key
                    ? group.Select(dataflow => ((Return<TOutput>) dataflow).Result)
                    : group.GroupBy(dataflow => dataflow.GetType())
                        .SelectMany(TransformContinuation));

        }

        private static IEnumerable<TOutput> TransformContinuation<TOutput>(IGrouping<Type, Dataflow<TOutput>> group)
        {
            if (group.Key == typeof (Continuation<TOutput>))
            {
                return TransformDataflow(group.Select(dataflow => ((Continuation<TOutput>)dataflow).Func()));
            }
            throw new NotImplementedException();
        }
    }
}