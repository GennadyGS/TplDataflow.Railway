using System;
using System.Collections.Generic;

namespace TplDataFlow.Extensions
{
    public static class ObservableExtensions
    {
        public static IObservable<Result<IList<TSuccess>, TFailure>> BufferSafe<TSuccess, TFailure>(
            this IObservable<Result<TSuccess, TFailure>> source, TimeSpan timeSpan, int count)
        {
            throw new NotImplementedException();
        }
    }
}