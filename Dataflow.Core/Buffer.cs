using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public class Buffer<T> : DataflowOperator<IList<T>>
    {
        private static readonly DataflowOperatorType<IList<T>> DataflowType = new BufferType<T>();

        public T Item { get; }

        public int BatchMaxSize { get; }

        public TimeSpan BatchTimeout { get; }

        public Buffer(IDataflowFactory dataflowFactory, T item, TimeSpan batchTimeout, int batchMaxSize) 
            : base(dataflowFactory)
        {
            Item = item;
            BatchTimeout = batchTimeout;
            BatchMaxSize = batchMaxSize;
        }

        public override DataflowOperatorType<IList<T>> GetDataflowOperatorType()
        {
            return DataflowType;
        }
    }
}