using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public static class EnumerableDataFlowExtensions
    {
        public static IEnumerable<int> BindDataflow(this IEnumerable<int> input, Func<int, Dataflow<int, int>> bindFunc)
        {
            return input
                .Select(bindFunc)
                .Select(dataflow =>
                {
                    if (dataflow is Return<int, int>)
                    {
                        var returnDataflow = (Return<int, int>) dataflow;
                        return returnDataflow.Result;
                    }
                    throw new NotImplementedException();
                });
        }
    }
}