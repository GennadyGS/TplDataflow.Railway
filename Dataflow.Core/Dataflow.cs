using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public abstract class Dataflow<T>
    {
        public abstract DataflowType<T> GetDataflowType();

        public abstract Dataflow<TOutput> Bind<TOutput>(Func<T, Dataflow<TOutput>> bindFunc);
    }

    public static class Dataflow
    {
        public static Dataflow<TOutput> Calculation<TInput, TOutput>(DataflowOperator<TInput> @operator,
            Func<TInput, Dataflow<TOutput>> continuation)
        {
            return new DataflowCalculation<TInput, TOutput>(@operator, continuation);
        }

        public static Return<T> Return<T>(T value)
        {
            return new Return<T>(value);
        }

        public static ReturnMany<T> ReturnMany<T>(IEnumerable<T> value)
        {
            return new ReturnMany<T>(value);
        }

        public static Buffer<T> Buffer<T>(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            return new Buffer<T>(item, batchTimeout, batchMaxSize);
        }
    }
}