using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public class CombinedPropagatorBlock<TInput, TMedium, TOutput> : IPropagatorBlock<TInput, TOutput>, IReceivableSourceBlock<TOutput>
    {
        private readonly IPropagatorBlock<TInput, TMedium> _previous;
        private readonly IPropagatorBlock<TMedium, TOutput> _next;

        public CombinedPropagatorBlock(IPropagatorBlock<TInput, TMedium> previous, IPropagatorBlock<TMedium, TOutput> next)
        {
            _previous = previous;
            _next = next;

            _previous.LinkTo(_next, new DataflowLinkOptions { PropagateCompletion = true });
        }

        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source,
            bool consumeToAccept)
        {
            return _previous.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        void IDataflowBlock.Complete()
        {
            _previous.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _previous.Fault(exception);
        }

        Task IDataflowBlock.Completion
        {
            get
            {
                return _next.Completion;
            }
        }

        IDisposable ISourceBlock<TOutput>.LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            return _next.LinkTo(target, linkOptions);
        }

        TOutput ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        {
            return _next.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            return _next.ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            _next.ReleaseReservation(messageHeader, target);
        }

        bool IReceivableSourceBlock<TOutput>.TryReceive(Predicate<TOutput> filter, out TOutput item)
        {
            return ((IReceivableSourceBlock<TOutput>)_next).TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<TOutput>.TryReceiveAll(out IList<TOutput> items)
        {
            return ((IReceivableSourceBlock<TOutput>)_next).TryReceiveAll(out items);
        }
    }
}