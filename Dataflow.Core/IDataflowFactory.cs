using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public interface IDataflowFactory
    {
        IDataflow<TOutput> Calculation<TInput, TOutput>(DataflowOperator<TInput> @operator,
            Func<TInput, IDataflow<TOutput>> continuation);

        IDataflow<T> Return<T>(T value);

        IDataflow<T> ReturnMany<T>(IEnumerable<T> value);

        IDataflow<IList<T>> Buffer<T>(T item, TimeSpan batchTimeout, int batchMaxSize);

        IDataflow<IGroupedDataflow<TKey, TElement>> GroupBy<TKey, TElement>(TElement item, Func<TElement, TKey> keySelector);

        IDataflow<IList<T>> ToList<T>(T item);
    }
}