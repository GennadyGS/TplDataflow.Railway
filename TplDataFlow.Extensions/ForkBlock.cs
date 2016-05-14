using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public class ForkBlock<TInput, TOutput1, TOutput2> : ITargetBlock<TInput>
    {
        private readonly ITargetBlock<TInput> _input;

        private readonly IPropagatorBlock<TOutput1, TOutput1> _output1 = new BufferBlock<TOutput1>();
        private readonly IPropagatorBlock<TOutput2, TOutput2> _output2 = new BufferBlock<TOutput2>();

        public ForkBlock(Func<TInput, Task<Tuple<TOutput1, TOutput2>>> transform)
        {
            _input = new ActionBlock<TInput>(CreateTransformActionAsync(transform));

            PropagateCompletion();
        }

        public ForkBlock(Func<TInput, Tuple<TOutput1, TOutput2>> transform)
        {
            _input = new ActionBlock<TInput>(CreateTransformActionSync(transform));

            PropagateCompletion();
        }

        public ISourceBlock<TOutput1> Output1
        {
            get
            {
                return _output1;
            }
        }

        public ISourceBlock<TOutput2> Output2
        {
            get
            {
                return _output2;
            }
        }

        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue,
            ISourceBlock<TInput> source, bool consumeToAccept)
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
                return Task.WhenAll(_output1.Completion, _output2.Completion);
            }
        }

        public ITargetBlock<TInput> ForkTo(ITargetBlock<TOutput1> target1, ITargetBlock<TOutput2> target2)
        {
            Output1.LinkWith(target1);
            Output2.LinkWith(target2);
            return this;
        }

        private Action<TInput> CreateTransformActionSync(Func<TInput, Tuple<TOutput1, TOutput2>> transformFunc)
        {
            return value => PropagateResult(transformFunc(value));
        }

        private Func<TInput, Task> CreateTransformActionAsync(
            Func<TInput, Task<Tuple<TOutput1, TOutput2>>> transformFunc)
        {
            return async value => await PropagateResultAsync(await transformFunc(value));
        }

        private void PropagateResult(Tuple<TOutput1, TOutput2> result)
        {
            _output1.Post(result.Item1);
            _output2.Post(result.Item2);
        }

        private async Task PropagateResultAsync(Tuple<TOutput1, TOutput2> result)
        {
            await Task.WhenAll(
                _output1.SendAsync(result.Item1),
                _output2.SendAsync(result.Item2));
        }

        private void PropagateCompletion()
        {
            _input.PropagateCompletion(_output1, _output2);
        }
    }
}