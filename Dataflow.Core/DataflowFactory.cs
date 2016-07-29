using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        IDataflow<TOutput> IDataflowFactory.Calculation<TInput, TOutput, TDataflowOperator>(TDataflowOperator @operator, Func<TInput, IDataflow<TOutput>> continuation)
        {
            var type = GetOrCreateType(typeof(DataflowCalculation<TInput, TOutput, TDataflowOperator>),
                () => _typeFactory.CreateCalculationType<TInput, TOutput, TDataflowOperator>());
            return new DataflowCalculation<TInput,TOutput, TDataflowOperator>(this, type, @operator, continuation);
        }

        IDataflow<T> IDataflowFactory.Return<T>(T value)
        {
            var type = GetOrCreateType(typeof(Return<T>),
                () => _typeFactory.CreateReturnType<T>());
            return new Return<T>(this, type, value);
        }

        IDataflow<T> IDataflowFactory.ReturnAsync<T>(Task<T> task)
        {
            var type = GetOrCreateType(typeof(ReturnAsync<T>),
                () => _typeFactory.CreateReturnAsyncType<T>());
            return new ReturnAsync<T>(this, type, task);
        }

        IDataflow<T> IDataflowFactory.ReturnMany<T>(IEnumerable<T> value)
        {
            var type = GetOrCreateType(typeof(ReturnMany<T>),
                () => _typeFactory.CreateReturnManyType<T>());
            return new ReturnMany<T>(this, type, value);
        }

        IDataflow<IList<T>> IDataflowFactory.Buffer<T>(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            var type = GetOrCreateType(typeof(Buffer<T>),
                () => _typeFactory.CreateBufferType<T>());
            return new Buffer<T>(this, type, item, batchTimeout, batchMaxSize);
        }

        public IDataflow<IGroupedDataflow<TKey, TElement>> GroupBy<TKey, TElement>(TElement item, Func<TElement, TKey> keySelector)
        {
            var type = GetOrCreateType(typeof(Group<TKey, TElement>),
                () => _typeFactory.CreateGroupType<TKey, TElement>());
            return new Group<TKey, TElement>(this, type, item, keySelector);
        }

        public IDataflow<IList<T>> ToList<T>(T item)
        {
            var type = GetOrCreateType(typeof(ToList<T>),
                () => _typeFactory.CreateToListType<T>());
            return new ToList<T>(this, type, item);
        }

        protected IDataflowType<TOutput> GetOrCreateType<TOutput>(Type typeOfDataflow, Func<IDataflowType<TOutput>> typeFactory)
        {
            return (IDataflowType<TOutput>)_typeCache.GetOrAdd(typeOfDataflow, typeFactory());
        }
    }
}