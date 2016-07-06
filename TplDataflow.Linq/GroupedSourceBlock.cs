using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataflow.Linq
{
    public class GroupedSourceBlock<TKey, TElement> : ISourceBlock<TElement>
    {
        private readonly IPropagatorBlock<TElement, TElement> _bufferBlock;

        public GroupedSourceBlock(TKey key)
        {
            Key = key;
            _bufferBlock = new BufferBlock<TElement>();
        }

        public TKey Key { get; }

        internal void Post(TElement value)
        {
            _bufferBlock.Post(value);
        }

        void IDataflowBlock.Complete()
        {
            _bufferBlock.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _bufferBlock.Fault(exception);
        }

        Task IDataflowBlock.Completion => _bufferBlock.Completion;

        IDisposable ISourceBlock<TElement>.LinkTo(ITargetBlock<TElement> target, DataflowLinkOptions linkOptions)
        {
            return _bufferBlock.LinkTo(target, linkOptions);
        }

        TElement ISourceBlock<TElement>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target, out bool messageConsumed)
        {
            return _bufferBlock.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<TElement>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target)
        {
            return _bufferBlock.ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<TElement>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target)
        {
            _bufferBlock.ReleaseReservation(messageHeader, target);
        }
    }
}