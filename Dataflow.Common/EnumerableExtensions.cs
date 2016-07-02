using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Common
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IList<T>> Buffer<T>(this IEnumerable<T> source, TimeSpan batchTimeout, int count)
        {
            return source
                .Select((value, index) => new {value, index})
                .GroupBy(item => item.index/count)
                .Select(group => group.Select(item => item.value).ToList());
        }
    }
}