using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collection.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<Task<TResult>> SelectMany<TSource, TResult>(this IEnumerable<Task<TSource>> source,
            Func<TSource, IEnumerable<TResult>> selector)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<IList<T>> ToListEnumerable<T>(this IEnumerable<T> source)
        {
            return source
                .GroupBy(_ => true)
                .Select(group => group.ToList());
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