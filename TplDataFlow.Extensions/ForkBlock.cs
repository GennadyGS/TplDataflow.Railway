using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public class ForkBlock<TInput, TOutputLeft, TOutputRight> : ITargetBlock<TInput>
    {
        private readonly ITargetBlock<TInput> _input;

        private readonly IPropagatorBlock<TOutputLeft, TOutputLeft> _leftOutput = new BufferBlock<TOutputLeft>();
        private readonly IPropagatorBlock<TOutputRight, TOutputRight> _rightOutput = new BufferBlock<TOutputRight>();

        public ForkBlock(Func<TInput, Task<Tuple<TOutputLeft, TOutputRight>>> transform)
        {
            _input = new ActionBlock<TInput>(CreateTransformActionAsync(transform));

            PropagateCompletion();
        }

        public ForkBlock(Func<TInput, Tuple<TOutputLeft, TOutputRight>> transform)
        {
            _input = new ActionBlock<TInput>(CreateTransformActionSync(transform));

            PropagateCompletion();
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

        public ITargetBlock<TInput> ForkTo(ITargetBlock<TOutputLeft> targetLeft, ITargetBlock<TOutputRight> targetRight)
        {
            LeftOutput.LinkWith(targetLeft);
            RightOutput.LinkWith(targetRight);
            return this;
        }

        private Action<TInput> CreateTransformActionSync(Func<TInput, Tuple<TOutputLeft, TOutputRight>> transformFunc)
        {
            return value => PropagateResultAsync(transformFunc(value)).Wait();
        }

        private Func<TInput, Task> CreateTransformActionAsync(Func<TInput, Task<Tuple<TOutputLeft, TOutputRight>>> transformFunc)
        {
            return async value => await PropagateResultAsync(await transformFunc(value));
        }

        private async Task PropagateResultAsync(Tuple<TOutputLeft, TOutputRight> result)
        {
            await Task.WhenAll(
                _leftOutput.SendAsync(result.Item1),
                _rightOutput.SendAsync(result.Item2));
        }

        private void PropagateCompletion()
        {
            _input.PropagateCompletion(_leftOutput, _rightOutput);
        }
    }
}