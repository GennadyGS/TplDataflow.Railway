using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public class DataflowFactory : IDataflowFactory
    {
        private readonly IDataflowTypeFactory _typeFactory;
        private readonly ConcurrentDictionary<Type, object> _typeCache = new ConcurrentDictionary<Type, object>();

        public DataflowFactory(IDataflowTypeFactory typeFactory)
        {
            _typeFactory = typeFactory;
        }

        IDataflow<TOutput> IDataflowFactory.Calculation<TInput, TOutput>(DataflowOperator<TInput> @operator, Func<TInput, IDataflow<TOutput>> continuation)
        {
            var type = (IDataflowType<TOutput>)_typeCache.GetOrAdd(
                typeof(DataflowCalculation<TInput, TOutput>),
                _ => _typeFactory.CreateCalculationType<TInput, TOutput>());
            return new DataflowCalculation<TInput,TOutput>(this, type, @operator, continuation);
        }

        IDataflow<T> IDataflowFactory.Return<T>(T value)
        {
            var type = (IDataflowType<T>)_typeCache.GetOrAdd(
                typeof(Return<T>),
                _ => _typeFactory.CreateReturnType<T>());
            return new Return<T>(this, type, value);
        }

        IDataflow<T> IDataflowFactory.ReturnMany<T>(IEnumerable<T> value)
        {
            var type = (IDataflowType<T>)_typeCache.GetOrAdd(
                typeof(ReturnMany<T>),
                _ => _typeFactory.CreateReturnManyType<T>());
            return new ReturnMany<T>(this, type, value);
        }

        IDataflow<IList<T>> IDataflowFactory.Buffer<T>(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            var type = (IDataflowType<IList<T>>)_typeCache.GetOrAdd(
                typeof(Buffer<T>),
                _ => _typeFactory.CreateBufferType<T>());
            return new Buffer<T>(this, type, item, batchTimeout, batchMaxSize);
        }
    }
}