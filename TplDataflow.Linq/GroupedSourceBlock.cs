using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataflow.Linq
{
    public class GroupedSourceBlock<TKey, TElement> : ISourceBlock<TElement>
    {
        private readonly IPropagatorBlock<TElement, TElement> bufferBlock;

        public GroupedSourceBlock(TKey key)
        {
            Key = key;
            bufferBlock = new BufferBlock<TElement>();
        }

        public TKey Key { get; }

        internal void Post(TElement value)
        {
            bufferBlock.Post(value);
        }

        void IDataflowBlock.Complete()
        {
            bufferBlock.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            bufferBlock.Fault(exception);
        }

        Task IDataflowBlock.Completion => bufferBlock.Completion;

        IDisposable ISourceBlock<TElement>.LinkTo(ITargetBlock<TElement> target, DataflowLinkOptions linkOptions)
        {
            return bufferBlock.LinkTo(target, linkOptions);
        }

        TElement ISourceBlock<TElement>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target, out bool messageConsumed)
        {
            return bufferBlock.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<TElement>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target)
        {
            return bufferBlock.ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<TElement>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TElement> target)
        {
            bufferBlock.ReleaseReservation(messageHeader, target);
        }
    }
}