using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public static class EnumerableDataFlowExtensions
    {
        public static IEnumerable<int> BindDataflow(this IEnumerable<int> input, Func<int, Dataflow<int, int>> bindFunc)
        {
            throw new NotImplementedException();
        }
    }
}