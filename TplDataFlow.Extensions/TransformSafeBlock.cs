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
        private readonly IPropagatorBlock<Result<TOutput, TFailure>, Result<TOutput, TFailure>> _outputBufferBlock =
            new BufferBlock<Result<TOutput, TFailure>>();

        private readonly IPropagatorBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>> _transformFailureBlock = new TransformBlock
            <Result<TInput, TFailure>, Result<TOutput, TFailure>>(
            result => Result.Failure<TOutput, TFailure>(result.Failure));

        private readonly IPropagatorBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>> _transformSuccessBlock;

        public TransformSafeBlock(Func<TInput, TOutput> transform) :
            this(new TransformBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>>(
                    input => transform(input.Success)))
        {
        }

        public TransformSafeBlock(Func<TInput, Result<TOutput, TFailure>> transform)
            : this(new TransformBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>>(
                    input => transform(input.Success)))
        {
        }

        public TransformSafeBlock(Func<TInput, IEnumerable<Result<TOutput, TFailure>>> transform)
            : this(new TransformManyBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>>(
                input => transform(input.Success)))
        {
        }

        private TransformSafeBlock(IPropagatorBlock<Result<TInput, TFailure>, Result<TOutput, TFailure>> transformSuccessBlock)
        {
            _transformSuccessBlock = transformSuccessBlock;

            _transformSuccessBlock.LinkTo(_outputBufferBlock);
            _transformFailureBlock.LinkTo(_outputBufferBlock);

            Task.WhenAll(_transformSuccessBlock.Completion, _transformFailureBlock.Completion)
                .ContinueWith(task => PropagateCompletion(task, _outputBufferBlock));
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
            return ((ISourceBlock<Result<TOutput, TFailure>>)_outputBufferBlock).LinkTo(target, linkOptions);
        }

        Result<TOutput, TFailure> ISourceBlock<Result<TOutput, TFailure>>.ConsumeMessage(
            DataflowMessageHeader messageHeader, ITargetBlock<Result<TOutput, TFailure>> target,
            out bool messageConsumed)
        {
            return _outputBufferBlock.ConsumeMessage(messageHeader, target,
                out messageConsumed);
        }

        bool ISourceBlock<Result<TOutput, TFailure>>.ReserveMessage(DataflowMessageHeader messageHeader,
            ITargetBlock<Result<TOutput, TFailure>> target)
        {
            return _outputBufferBlock.ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<Result<TOutput, TFailure>>.ReleaseReservation(DataflowMessageHeader messageHeader,
            ITargetBlock<Result<TOutput, TFailure>> target)
        {
            _outputBufferBlock.ReleaseReservation(messageHeader, target);
        }

        bool IReceivableSourceBlock<Result<TOutput, TFailure>>.TryReceive(Predicate<Result<TOutput, TFailure>> filter,
            out Result<TOutput, TFailure> item)
        {
            return ((IReceivableSourceBlock<Result<TOutput, TFailure>>)_outputBufferBlock).TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<Result<TOutput, TFailure>>.TryReceiveAll(out IList<Result<TOutput, TFailure>> items)
        {
            return ((IReceivableSourceBlock<Result<TOutput, TFailure>>)_outputBufferBlock).TryReceiveAll(out items);
        }

        public static void PropagateCompletion(Task task, IDataflowBlock targetBlock)
        {
            if (task.IsFaulted)
                targetBlock.Fault(task.Exception);
            else
                targetBlock.Complete();
        }
    }
}