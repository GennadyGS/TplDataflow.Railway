using LanguageExt;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Railway.Linq;
using TplDataFlow.Extensions;

namespace TplDataflow.Railway
{
    public class TransformSafeBlock<TLeft, TRightInput, TRightOutput> : 
        IPropagatorBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>,
        IReceivableSourceBlock<Either<TLeft, TRightOutput>>
    {
        private readonly JointPointBlock<Either<TLeft, TRightOutput>> outputBufferBlock =
            new JointPointBlock<Either<TLeft, TRightOutput>>();

        private readonly IPropagatorBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>> transformLeftBlock = 
            new TransformBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>(input => input.GetLeftSafe());

        private readonly IPropagatorBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>> transformRightBlock;

        public TransformSafeBlock(Func<TRightInput, TRightOutput> transform) :
            this(new TransformBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>(
                input => transform(input.GetRightSafe())))
        {
        }

        public TransformSafeBlock(Func<TRightInput, Either<TLeft, TRightOutput>> transform)
            : this(new TransformBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>(
                input => transform(input.GetRightSafe())))
        {
        }

        public TransformSafeBlock(Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> transform)
            : this(new TransformManyBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>(
                input => transform(input.GetRightSafe())))
        {
        }

        private TransformSafeBlock(IPropagatorBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>> transformRightBlock)
        {
            this.transformRightBlock = transformRightBlock;

            this.transformRightBlock.LinkTo(outputBufferBlock.AddInput(), new DataflowLinkOptions { PropagateCompletion = true });
            transformLeftBlock.LinkTo(outputBufferBlock.AddInput(), new DataflowLinkOptions { PropagateCompletion = true });
        }

        DataflowMessageStatus ITargetBlock<Either<TLeft, TRightInput>>.OfferMessage(DataflowMessageHeader messageHeader,
            Either<TLeft, TRightInput> messageValue, ISourceBlock<Either<TLeft, TRightInput>> source,
            bool consumeToAccept)
        {
            return messageValue
                .Match(
                    right => transformRightBlock,
                    left => transformLeftBlock)
                .OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        void IDataflowBlock.Complete()
        {
            transformRightBlock.Complete();
            transformLeftBlock.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            transformRightBlock.Fault(exception);
            transformLeftBlock.Fault(exception);
        }

        Task IDataflowBlock.Completion
        {
            get
            {
                return outputBufferBlock.Completion;
            }
        }

        IDisposable ISourceBlock<Either<TLeft, TRightOutput>>.LinkTo(ITargetBlock<Either<TLeft, TRightOutput>> target,
            DataflowLinkOptions linkOptions)
        {
            return ((ISourceBlock<Either<TLeft, TRightOutput>>)outputBufferBlock)
                .LinkTo(target, linkOptions);
        }

        Either<TLeft, TRightOutput> ISourceBlock<Either<TLeft, TRightOutput>>.ConsumeMessage(
            DataflowMessageHeader messageHeader, ITargetBlock<Either<TLeft, TRightOutput>> target,
            out bool messageConsumed)
        {
            return ((ISourceBlock<Either<TLeft, TRightOutput>>)outputBufferBlock)
                .ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<Either<TLeft, TRightOutput>>.ReserveMessage(DataflowMessageHeader messageHeader,
            ITargetBlock<Either<TLeft, TRightOutput>> target)
        {
            return ((ISourceBlock<Either<TLeft, TRightOutput>>)outputBufferBlock)
                .ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<Either<TLeft, TRightOutput>>.ReleaseReservation(DataflowMessageHeader messageHeader,
            ITargetBlock<Either<TLeft, TRightOutput>> target)
        {
            ((ISourceBlock<Either<TLeft, TRightOutput>>)outputBufferBlock)
                .ReleaseReservation(messageHeader, target);
        }

        bool IReceivableSourceBlock<Either<TLeft, TRightOutput>>.TryReceive(Predicate<Either<TLeft, TRightOutput>> filter,
            out Either<TLeft, TRightOutput> item)
        {
            return ((IReceivableSourceBlock<Either<TLeft, TRightOutput>>)outputBufferBlock).TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<Either<TLeft, TRightOutput>>.TryReceiveAll(out IList<Either<TLeft, TRightOutput>> items)
        {
            return ((IReceivableSourceBlock<Either<TLeft, TRightOutput>>)outputBufferBlock).TryReceiveAll(out items);
        }
    }
}