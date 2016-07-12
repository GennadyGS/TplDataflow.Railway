using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public class Buffer<T> : DataflowOperator<IList<T>, Buffer<T>>
    {
        public T Item { get; }

        public int BatchMaxSize { get; }

        public TimeSpan BatchTimeout { get; }

        public Buffer(IDataflowFactory factory, IDataflowType<IList<T>> type, 
            T item, TimeSpan batchTimeout, int batchMaxSize) 
            : base(factory, type)
        {
            Item = item;
            BatchTimeout = batchTimeout;
            BatchMaxSize = batchMaxSize;
        }
    }
}