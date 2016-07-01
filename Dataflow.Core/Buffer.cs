using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public class Buffer<T> : DataflowOperator<IList<T>>
    {
        public T Item { get; }

        public int BatchMaxSize { get; }

        public TimeSpan BatchTimeout { get; }

        public Buffer(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            Item = item;
            BatchTimeout = batchTimeout;
            BatchMaxSize = batchMaxSize;
        }

        public override DataflowType<IList<T>> GetDataflowType()
        {
            return new BufferType<T>();
        }
    }
}