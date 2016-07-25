using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt.Trans;

namespace Collection.Extensions
{
    public static class EnumerableExtensions
    {
        public static Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> selector)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<Task<TResult>> SelectMany<TSource, TResult>(this IEnumerable<Task<TSource>> source,
            Func<TSource, IEnumerable<TResult>> selector)
        {
            return source
                .MapT(selector)
                .SelectMany(item => item.ToEnumerableOfTasks());
        }

        public static Task<IEnumerable<TResult>> SelectManyAsync<TSource, TResult>(this IEnumerable<TSource> source,
            Func<TSource, IEnumerable<Task<TResult>>> selector)
        {
            throw new NotImplementedException();
        }

        public static Task<IEnumerable<TResult>> SelectManyAsync<TSource, TResult>(this IEnumerable<TSource> source,
            Func<TSource, Task<IEnumerable<TResult>>> selector)
        {
            throw new NotImplementedException();
        }

        public static Task<IEnumerable<TResult>> SelectManyAsync<TSource, TResult>(this Task<IEnumerable<TSource>> source,
            Func<TSource, IEnumerable<TResult>> selector)
        {
            throw new NotImplementedException();
        }

        public static Task<IEnumerable<IGrouping<TKey, TSource>>> GroupByAsync<TSource, TKey>(
            this Task<IEnumerable<TSource>> source,
            Func<TSource, TKey> keySelector)
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

        private static IEnumerable<Task<T>> ToEnumerableOfTasks<T>(this Task<IEnumerable<T>> source)
        {
            throw new NotImplementedException();
        }
    }
}