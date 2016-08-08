using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dataflow.Core
{
    public class DataflowFactory : IDataflowFactory
    {
        private readonly IDataflowTypeFactory typeFactory;
        private readonly ConcurrentDictionary<Type, object> typeCache = new ConcurrentDictionary<Type, object>();

        public DataflowFactory(IDataflowTypeFactory typeFactory)
        {
            this.typeFactory = typeFactory;
        }

        IDataflow<TOutput> IDataflowFactory.Calculation<TInput, TOutput, TDataflowOperator>(TDataflowOperator @operator, Func<TInput, IDataflow<TOutput>> continuation)
        {
            var type = GetOrCreateType(typeof(DataflowCalculation<TInput, TOutput, TDataflowOperator>),
                () => typeFactory.CreateCalculationType<TInput, TOutput, TDataflowOperator>());
            return new DataflowCalculation<TInput,TOutput, TDataflowOperator>(this, type, @operator, continuation);
        }

        IDataflow<T> IDataflowFactory.Return<T>(T value)
        {
            var type = GetOrCreateType(typeof(Return<T>),
                () => typeFactory.CreateReturnType<T>());
            return new Return<T>(this, type, value);
        }

        IDataflow<T> IDataflowFactory.ReturnAsync<T>(Task<T> task)
        {
            var type = GetOrCreateType(typeof(ReturnAsync<T>),
                () => typeFactory.CreateReturnAsyncType<T>());
            return new ReturnAsync<T>(this, type, task);
        }

        IDataflow<T> IDataflowFactory.ReturnMany<T>(IEnumerable<T> value)
        {
            var type = GetOrCreateType(typeof(ReturnMany<T>),
                () => typeFactory.CreateReturnManyType<T>());
            return new ReturnMany<T>(this, type, value);
        }

        IDataflow<T> IDataflowFactory.ReturnManyAsync<T>(Task<IEnumerable<T>> task)
        {
            var type = GetOrCreateType(typeof(ReturnManyAsync<T>),
                () => typeFactory.CreateReturnManyAsyncType<T>());
            return new ReturnManyAsync<T>(this, type, task);
        }

        IDataflow<IList<T>> IDataflowFactory.Buffer<T>(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            var type = GetOrCreateType(typeof(Buffer<T>),
                () => typeFactory.CreateBufferType<T>());
            return new Buffer<T>(this, type, item, batchTimeout, batchMaxSize);
        }

        public IDataflow<IGroupedDataflow<TKey, TElement>> GroupBy<TKey, TElement>(TElement item, Func<TElement, TKey> keySelector)
        {
            var type = GetOrCreateType(typeof(Group<TKey, TElement>),
                () => typeFactory.CreateGroupType<TKey, TElement>());
            return new Group<TKey, TElement>(this, type, item, keySelector);
        }

        public IDataflow<IList<T>> ToList<T>(T item)
        {
            var type = GetOrCreateType(typeof(ToList<T>),
                () => typeFactory.CreateToListType<T>());
            return new ToList<T>(this, type, item);
        }

        protected IDataflowType<TOutput> GetOrCreateType<TOutput>(Type typeOfDataflow, Func<IDataflowType<TOutput>> typeFactory)
        {
            return (IDataflowType<TOutput>)typeCache.GetOrAdd(typeOfDataflow, typeFactory());
        }
    }
}