using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public interface IDataflowFactory
    {
        Dataflow<TOutput> Calculation<TInput, TOutput>(DataflowOperator<TInput> @operator,
            Func<TInput, Dataflow<TOutput>> continuation);

        Return<T> Return<T>(T value);

        ReturnMany<T> ReturnMany<T>(IEnumerable<T> value);

        Buffer<T> Buffer<T>(T item, TimeSpan batchTimeout, int batchMaxSize);
    }
}