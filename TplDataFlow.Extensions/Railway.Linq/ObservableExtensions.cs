using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using LanguageExt;
using static LanguageExt.Prelude;

namespace TplDataFlow.Extensions.Railway.Linq
{
    public static class ObservableExtensions
    {
        public static IObservable<Either<TLeft, IList<TRight>>> BufferSafe<TLeft, TRight>(
            this IObservable<Either<TLeft, TRight>> source, TimeSpan timeSpan, int count)
        {
            return source
                .Buffer(count)
                .SelectMany(batch => batch
                    .GroupBy(item => item.IsRight)
                    .SelectMany(group => group.Key
                        ? List(
                            Right<TLeft, IList<TRight>>(
                                group.Rights().ToList()))
                        : group
                            .Lefts()
                            .Select(Left<TLeft, IList<TRight>>)));
        }
    }
}