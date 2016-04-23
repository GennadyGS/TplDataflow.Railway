using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    internal class SafeTransformBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>, IReceivableSourceBlock<TOutput>
    {
        private readonly Func<TInput, Task<TOutput>> _transform;

        private readonly ITargetBlock<TInput> _transformActionBlock;
        private readonly BufferBlock<TOutput> _outputBufferBlock = new BufferBlock<TOutput>();
        private readonly BufferBlock<Tuple<Exception, TInput>> _exceptionBufferBlock = new BufferBlock<Tuple<Exception, TInput>>();

        public SafeTransformBlock(Func<TInput, Task<TOutput>> transform)
        {
            _transform = transform;

            _transformActionBlock = new ActionBlock<TInput>(TransformAction);

            _transformActionBlock.PropagateCompletion(_outputBufferBlock, _exceptionBufferBlock);
        }

        private async Task TransformAction(TInput input)
        {
            try
            {
                _outputBufferBlock.Post(await _transform(input));
            }
            catch (Exception e)
            {
                _exceptionBufferBlock.Post(new Tuple<Exception, TInput>(e, input));
                throw;
            }
        }

        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, bool consumeToAccept)
        {
            return _transformActionBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        void IDataflowBlock.Complete()
        {
            _transformActionBlock.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _transformActionBlock.Fault(exception);
        }

        Task IDataflowBlock.Completion
        {
            get
            {
                return Task.WhenAll(_outputBufferBlock.Completion, _exceptionBufferBlock.Completion);
            }
        }

        IDisposable ISourceBlock<TOutput>.LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            return _outputBufferBlock.LinkTo(target, linkOptions);
        }

        TOutput ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        {
            return ((ISourceBlock<TOutput>)_outputBufferBlock).ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            return ((ISourceBlock<TOutput>)_outputBufferBlock).ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            ((ISourceBlock<TOutput>)_outputBufferBlock).ReleaseReservation(messageHeader, target);
        }

        bool IReceivableSourceBlock<TOutput>.TryReceive(Predicate<TOutput> filter, out TOutput item)
        {
            return _outputBufferBlock.TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<TOutput>.TryReceiveAll(out IList<TOutput> items)
        {
            return _outputBufferBlock.TryReceiveAll(out items);
        }

        public ISourceBlock<Tuple<Exception, TInput>> Exception
        {
            get
            {
                return _exceptionBufferBlock;
            }
        }
    }
}