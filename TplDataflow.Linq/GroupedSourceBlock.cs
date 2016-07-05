using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataflow.Linq
{
    public class GroupedSourceBlock<TKey, TElement> : ISourceBlock<TElement>
    {
        private ISourceBlock<TElement> _sourceBlock;

        public GroupedSourceBlock(TKey key, ISourceBlock<TElement> sourceBlock)
        {
            Key = key;
            _sourceBlock = sourceBlock;
        }

        public TKey Key { get; }
        void IDataflowBlock.Complete()
        {
            _sourceBlock.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _sourceBlock.Fault(exception);
        }

        Task IDataflowBlock.Completion
        {
            get
            {
                return _sourceBlock.Completion;
            }
        }

        IDisposable ISourceBlock<TElement>.LinkTo(ITargetBlock<TElement> target, DataflowLinkOptions linkOptions)
        {
            return _sourceBlock.LinkTo(target, linkOptions);
        }

        TElement ISourceBlock<TElement>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target, out bool messageConsumed)
        {
            return _sourceBlock.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<TElement>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target)
        {
            return _sourceBlock.ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<TElement>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target)
        {
            _sourceBlock.ReleaseReservation(messageHeader, target);
        }
    }
}