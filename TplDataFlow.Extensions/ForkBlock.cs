using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public class ForkBlock<TInput, TOutputLeft, TOutputRight> : ITargetBlock<TInput>
    {
        private readonly Func<TInput, Task<Tuple<TOutputLeft, TOutputRight>>> _transform;
        private readonly ITargetBlock<TInput> _input;

        private readonly IPropagatorBlock<TOutputLeft, TOutputLeft> _leftOutput = new BufferBlock<TOutputLeft>();
        private readonly IPropagatorBlock<TOutputRight, TOutputRight> _rightOutput = new BufferBlock<TOutputRight>();

        public ForkBlock(Func<TInput, Task<Tuple<TOutputLeft, TOutputRight>>> transform)
        {
            _transform = transform;

            _input = new ActionBlock<TInput>(TransformAction);

            _input.PropagateCompletion(_leftOutput, _rightOutput);
        }

        public ITargetBlock<TInput> ForkTo(ITargetBlock<TOutputLeft> targetLeft, ITargetBlock<TOutputRight> targetRight)
        {
            LeftOutput.LinkWith(targetLeft);
            RightOutput.LinkWith(targetRight);
            return this;
        }

        public ISourceBlock<TOutputLeft> LeftOutput
        {
            get
            {
                return _leftOutput;
            }
        }

        public ISourceBlock<TOutputRight> RightOutput
        {
            get
            {
                return _rightOutput;
            }
        }

        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, bool consumeToAccept)
        {
            return _input.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        void IDataflowBlock.Complete()
        {
            _input.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _input.Fault(exception);
        }

        Task IDataflowBlock.Completion
        {
            get
            {
                return Task.WhenAll(_leftOutput.Completion, _rightOutput.Completion);
            }
        }

        private async Task TransformAction(TInput value)
        {
            var result = await _transform(value);
            _leftOutput.Post(result.Item1);
            _rightOutput.Post(result.Item2);
        }
    }
}