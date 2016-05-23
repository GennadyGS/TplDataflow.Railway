using System.Collections.Generic;
using System.Linq;

namespace TplDataFlow.Extensions.Linq.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this T value)
        {
            yield return value;
        }

        public static IEnumerable<IList<T>> Buffer<T>(this IEnumerable<T> source, int count)
        {
            return source
                .Select((value, index) => new { value, index })
                .GroupBy(item => item.index / count)
                .Select(group => group.Select(item => item.value).ToList());
        }
    }
}
