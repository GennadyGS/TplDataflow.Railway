using System;

namespace Dataflow.Core
{
    public class Group<TKey, TElement> : DataflowOperator<IGroupedDataflow<TKey, TElement>>
    {
        public TElement Item { get; }

        public Func<TElement, TKey> KeySelector { get; }

        public Group(IDataflowFactory factory, IDataflowType<IGroupedDataflow<TKey, TElement>> type, 
            TElement item, Func<TElement, TKey> keySelector) 
            : base(factory, type)
        {
            Item = item;
            KeySelector = keySelector;
        }
    }
}