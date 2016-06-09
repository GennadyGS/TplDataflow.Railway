using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public static class EnumerableDataFlowExtensions
    {
        public static IEnumerable<TOutput> BindDataflow<TInput, TOutput>(this IEnumerable<TInput> input, Func<TInput, Dataflow<TOutput>> bindFunc)
        {
            return input
                .Select(bindFunc)
                .Select(dataflow =>
                {
                    if (dataflow is Return<TOutput>)
                    {
                        var returnDataflow = (Return<TOutput>) dataflow;
                        return returnDataflow.Result;
                    }
                    throw new NotImplementedException();
                });
        }
    }
}