using System.Collections.Generic;

namespace Dataflow.Core
{
    public class ToList<T> : DataflowOperator<IList<T>, ToList<T>>
    {
        public T Item { get; }

        public ToList(IDataflowFactory factory, IDataflowType<IList<T>> type, T item) : base(factory, type)
        {
            Item = item;
        }
    }
}