using System;
using System.Collections.Generic;
using System.Linq;

namespace Collection.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IList<T>> ToListEnumerable<T>(this IEnumerable<T> source)
        {
            return Enumerable.Repeat(source.ToList(), 1);
        }

        public static IEnumerable<IList<T>> Buffer<T>(this IEnumerable<T> source, TimeSpan batchTimeout, int count)
        {
            return source
                .Select((value, index) => new {value, index})
                .GroupBy(item => item.index/count)
                .Select(group => group.Select(item => item.value).ToList());
        }
    }
}