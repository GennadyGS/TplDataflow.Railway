using System.Collections.Generic;

namespace Dataflow.Core
{
    internal class GroupedDataflow<TKey, TElement> : ReturnMany<TElement>, IGroupedDataflow<TKey, TElement>
    {
        public GroupedDataflow(IDataflowFactory factory, IDataflowType<TElement> type, TKey key, IEnumerable<TElement> items) : base(factory, type, items)
        {
            Key = key;
        }

        public TKey Key { get; }
    }
}