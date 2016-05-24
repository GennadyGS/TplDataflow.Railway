using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using TplDataFlow.Extensions.Linq.Extensions;
using TplDataFlow.Extensions.Railway.Core;

namespace TplDataFlow.Extensions.Railway.Linq
{
    public static class ObservableExtensions
    {
        public static IObservable<Result<IList<TSuccess>, TFailure>> BufferSafe<TSuccess, TFailure>(
            this IObservable<Result<TSuccess, TFailure>> source, TimeSpan timeSpan, int count)
        {
            return source
                .Buffer(timeSpan, count)
                .SelectMany(batch => batch
                    .GroupBy(item => item.IsSuccess)
                    .SelectMany(group => group.Key
                        ? Result.Success<IList<TSuccess>, TFailure>(group.Select(item => item.Success).ToList()).AsEnumerable()
                        : group.Select(item => Result.Failure<IList<TSuccess>, TFailure>(item.Failure))));
        }
    }
}