using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LanguageExt;
using static LanguageExt.Prelude;

namespace TplDataFlow.Extensions.TplDataflow.Railway
{
    public class TransformSafeBlock<TLeft, TRightInput, TRightOutput> :
        IPropagatorBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>,
        IReceivableSourceBlock<Either<TLeft, TRightOutput>>
    {
        private readonly IPropagatorBlock<Either<TLeft, TRightOutput>, Either<TLeft, TRightOutput>> _outputBufferBlock =
            new BufferBlock<Either<TLeft, TRightOutput>>();

        private readonly IPropagatorBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>> _transformLeftBlock = 
            new TransformBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>(input => GetLeft(input));

        private readonly IPropagatorBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>> _transformRightBlock;

        public TransformSafeBlock(Func<TRightInput, TRightOutput> transform) :
            this(new TransformBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>(
                input => transform(GetRight(input))))
        {
        }

        public TransformSafeBlock(Func<TRightInput, Either<TLeft, TRightOutput>> transform)
            : this(new TransformBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>(
                input => transform(GetRight(input))))
        {
        }

        public TransformSafeBlock(Func<TRightInput, IEnumerable<Either<TLeft, TRightOutput>>> transform)
            : this(new TransformManyBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>>(
                input => transform(GetRight(input))))
        {
        }

        private TransformSafeBlock(IPropagatorBlock<Either<TLeft, TRightInput>, Either<TLeft, TRightOutput>> transformRightBlock)
        {
            _transformRightBlock = transformRightBlock;

            _transformRightBlock.LinkTo(_outputBufferBlock);
            _transformLeftBlock.LinkTo(_outputBufferBlock);

            Task.WhenAll(_transformRightBlock.Completion, _transformLeftBlock.Completion)
                .ContinueWith(task => PropagateCompletion(task, _outputBufferBlock));
        }

        DataflowMessageStatus ITargetBlock<Either<TLeft, TRightInput>>.OfferMessage(DataflowMessageHeader messageHeader,
            Either<TLeft, TRightInput> messageValue, ISourceBlock<Either<TLeft, TRightInput>> source,
            bool consumeToAccept)
        {
            return messageValue
                .Match(
                    right => _transformRightBlock,
                    left => _transformLeftBlock)
                .OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        void IDataflowBlock.Complete()
        {
            _transformRightBlock.Complete();
            _transformLeftBlock.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _transformRightBlock.Fault(exception);
            _transformLeftBlock.Fault(exception);
        }

        Task IDataflowBlock.Completion
        {
            get
            {
                return _outputBufferBlock.Completion;
            }
        }

        IDisposable ISourceBlock<Either<TLeft, TRightOutput>>.LinkTo(ITargetBlock<Either<TLeft, TRightOutput>> target,
            DataflowLinkOptions linkOptions)
        {
            return _outputBufferBlock.LinkTo(target, linkOptions);
        }

        Either<TLeft, TRightOutput> ISourceBlock<Either<TLeft, TRightOutput>>.ConsumeMessage(
            DataflowMessageHeader messageHeader, ITargetBlock<Either<TLeft, TRightOutput>> target,
            out bool messageConsumed)
        {
            return _outputBufferBlock.ConsumeMessage(messageHeader, target,
                out messageConsumed);
        }

        bool ISourceBlock<Either<TLeft, TRightOutput>>.ReserveMessage(DataflowMessageHeader messageHeader,
            ITargetBlock<Either<TLeft, TRightOutput>> target)
        {
            return _outputBufferBlock.ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<Either<TLeft, TRightOutput>>.ReleaseReservation(DataflowMessageHeader messageHeader,
            ITargetBlock<Either<TLeft, TRightOutput>> target)
        {
            _outputBufferBlock.ReleaseReservation(messageHeader, target);
        }

        bool IReceivableSourceBlock<Either<TLeft, TRightOutput>>.TryReceive(Predicate<Either<TLeft, TRightOutput>> filter,
            out Either<TLeft, TRightOutput> item)
        {
            return ((IReceivableSourceBlock<Either<TLeft, TRightOutput>>)_outputBufferBlock).TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<Either<TLeft, TRightOutput>>.TryReceiveAll(out IList<Either<TLeft, TRightOutput>> items)
        {
            return ((IReceivableSourceBlock<Either<TLeft, TRightOutput>>)_outputBufferBlock).TryReceiveAll(out items);
        }

        public static void PropagateCompletion(Task task, IDataflowBlock targetBlock)
        {
            if (task.IsFaulted)
                targetBlock.Fault(task.Exception);
            else
                targetBlock.Complete();
        }

        private static TRightInput GetRight(Either<TLeft, TRightInput> input)
        {
            return input.IfLeft(() => failwith<TRightInput>("Not in right state"));
        }

        private static TLeft GetLeft(Either<TLeft, TRightInput> input)
        {
            return input.IfRight(() => failwith<TLeft>("Not in left state"));
        }
    }
}