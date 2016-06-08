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
                    var returnDataflow = dataflow as Return<int, int>;
                    if (returnDataflow == null)
                    {
                        throw new NotImplementedException();
                    }
                    return returnDataflow.Result;
                });
        }
    }
}