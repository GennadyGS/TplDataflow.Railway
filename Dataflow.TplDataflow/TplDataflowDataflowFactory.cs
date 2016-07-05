using System;
using System.Collections.Generic;
using Dataflow.Core;

namespace Dataflow.TplDataflow
{
    internal class TplDataflowDataflowFactory : IDataflowFactory
    {
        Dataflow<TOutput> IDataflowFactory.Calculation<TInput, TOutput>(DataflowOperator<TInput> @operator, Func<TInput, Dataflow<TOutput>> continuation)
        {
            throw new NotImplementedException();
        }

        Return<T> IDataflowFactory.Return<T>(T value)
        {
            throw new NotImplementedException();
        }

        ReturnMany<T> IDataflowFactory.ReturnMany<T>(IEnumerable<T> value)
        {
            throw new NotImplementedException();
        }

        Buffer<T> IDataflowFactory.Buffer<T>(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            throw new NotImplementedException();
        }
    }
}