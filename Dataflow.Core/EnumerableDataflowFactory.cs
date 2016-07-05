using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    internal class EnumerableDataflowFactory : IDataflowFactory
    {
        public Dataflow<TOutput> Calculation<TInput, TOutput>(DataflowOperator<TInput> @operator, Func<TInput, Dataflow<TOutput>> continuation)
        {
            return new DataflowCalculation<TInput,TOutput>(this, @operator, continuation);
        }

        public Return<T> Return<T>(T value)
        {
            return new Return<T>(this, value);
        }

        public ReturnMany<T> ReturnMany<T>(IEnumerable<T> value)
        {
            return new ReturnMany<T>(this, value);
        }

        public Buffer<T> Buffer<T>(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            return new Buffer<T>(this, item, batchTimeout, batchMaxSize);
        }
    }
}