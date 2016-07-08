using System.Collections.Generic;

namespace Dataflow.Core
{
    public interface IDataflowTypeFactory
    {
        IDataflowType<TOutput> CreateCalculationType<TInput, TOutput>();

        IDataflowType<T> CreateReturnType<T>();

        IDataflowType<T> CreateReturnManyType<T>();

        IDataflowType<IList<T>> CreateBufferType<T>();

        IDataflowType<IGroupedDataflow<TKey, TElement>> CreateGroupType<TKey, TElement>();
    }
}