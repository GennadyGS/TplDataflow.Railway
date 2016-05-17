using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public class TransformSafeBlock<TInput, TOutput, TFailure> :
        IPropagatorBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>>,
        IReceivableSourceBlock<Result<TOutput, TFailure>>
    {
        private readonly JointPointBlock<Result<TOutput, TFailure>> _outputBufferBlock =
            new JointPointBlock<Result<TOutput, TFailure>>();

        private readonly IPropagatorBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>> _transformFailureBlock = new TransformBlock
            <Result<TInput, TFailure>, Result<TOutput, TFailure>>(
            result => Result.Failure<TOutput, TFailure>(result.Failure));

        private readonly IPropagatorBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>> _transformSuccessBlock;

        public TransformSafeBlock(Func<TInput, TOutput> transform)
        {
            _transformSuccessBlock =
                new TransformBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>>(
                    input => transform(input.Success));
            CreateDataFlow();
        }

        public TransformSafeBlock(Func<TInput, Result<TOutput, TFailure>> transform)
        {
            _transformSuccessBlock =
                new TransformBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>>(
                    input => transform(input.Success));
            CreateDataFlow();
        }

        DataflowMessageStatus ITargetBlock<Result<TInput, TFailure>>.OfferMessage(DataflowMessageHeader messageHeader,
            Result<TInput, TFailure> messageValue, ISourceBlock<Result<TInput, TFailure>> source,
            bool consumeToAccept)
        {
            if (messageValue.IsSuccess)
            {
                return _transformSuccessBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
            }
            return _transformFailureBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        void IDataflowBlock.Complete()
        {
            _transformSuccessBlock.Complete();
            _transformFailureBlock.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _transformSuccessBlock.Fault(exception);
            _transformFailureBlock.Fault(exception);
        }

        Task IDataflowBlock.Completion
        {
            get
            {
                return _outputBufferBlock.Completion;
            }
        }

        IDisposable ISourceBlock<Result<TOutput, TFailure>>.LinkTo(ITargetBlock<Result<TOutput, TFailure>> target,
            DataflowLinkOptions linkOptions)
        {
            return ((ISourceBlock<Result<TOutput, TFailure>>) _outputBufferBlock).LinkTo(target, linkOptions);
        }

        Result<TOutput, TFailure> ISourceBlock<Result<TOutput, TFailure>>.ConsumeMessage(
            DataflowMessageHeader messageHeader, ITargetBlock<Result<TOutput, TFailure>> target,
            out bool messageConsumed)
        {
            return ((ISourceBlock<Result<TOutput, TFailure>>) _outputBufferBlock).ConsumeMessage(messageHeader, target,
                out messageConsumed);
        }

        bool ISourceBlock<Result<TOutput, TFailure>>.ReserveMessage(DataflowMessageHeader messageHeader,
            ITargetBlock<Result<TOutput, TFailure>> target)
        {
            return ((ISourceBlock<Result<TOutput, TFailure>>) _outputBufferBlock).ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<Result<TOutput, TFailure>>.ReleaseReservation(DataflowMessageHeader messageHeader,
            ITargetBlock<Result<TOutput, TFailure>> target)
        {
            ((ISourceBlock<Result<TOutput, TFailure>>) _outputBufferBlock).ReleaseReservation(messageHeader, target);
        }

        bool IReceivableSourceBlock<Result<TOutput, TFailure>>.TryReceive(Predicate<Result<TOutput, TFailure>> filter,
            out Result<TOutput, TFailure> item)
        {
            return ((IReceivableSourceBlock<Result<TOutput, TFailure>>) _outputBufferBlock).TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<Result<TOutput, TFailure>>.TryReceiveAll(out IList<Result<TOutput, TFailure>> items)
        {
            return ((IReceivableSourceBlock<Result<TOutput, TFailure>>) _outputBufferBlock).TryReceiveAll(out items);
        }

        private void CreateDataFlow()
        {
            _transformSuccessBlock.LinkWith(_outputBufferBlock.AddInput());
            _transformFailureBlock.LinkWith(_outputBufferBlock.AddInput());
        }
    }
}