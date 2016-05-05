using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public class ForkManyBlock<TInput, TOutput1, TOutput2> : ITargetBlock<TInput>
    {
        private readonly ITargetBlock<TInput> _input;

        private readonly IPropagatorBlock<TOutput1, TOutput1> _output1 = new BufferBlock<TOutput1>();
        private readonly IPropagatorBlock<TOutput2, TOutput2> _output2 = new BufferBlock<TOutput2>();

        public ForkManyBlock(Func<TInput, Task<Tuple<IEnumerable<TOutput1>, IEnumerable<TOutput2>>>> transform)
        {
            _input = new ActionBlock<TInput>(CreateTransformActionAsync(transform));

            PropagateCompletion();
        }

        public ForkManyBlock(Func<TInput, Tuple<IEnumerable<TOutput1>, IEnumerable<TOutput2>>> transform)
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
                return Task.WhenAll(_output1.Completion, _output2.Completion);
            }
        }

        public ITargetBlock<TInput> ForkTo(ITargetBlock<TOutput1> target1, ITargetBlock<TOutput2> target2)
        {
            Output1.LinkWith(target1);
            Output2.LinkWith(target2);
            return this;
        }

        private Action<TInput> CreateTransformActionSync(Func<TInput, Tuple<IEnumerable<TOutput1>, IEnumerable<TOutput2>>> transformFunc)
        {
            return value => PropagateResult(transformFunc(value));
        }

        private Func<TInput, Task> CreateTransformActionAsync(Func<TInput, Task<Tuple<IEnumerable<TOutput1>, IEnumerable<TOutput2>>>> transformFunc)
        {
            return async value => await PropagateResultAsync(await transformFunc(value));
        }

        private void PropagateResult(Tuple<IEnumerable<TOutput1>, IEnumerable<TOutput2>> result)
        {
            foreach (var item in result.Item1)
            {
                _output1.Post(item);
            }
            foreach (var item in result.Item2)
            {
                _output2.Post(item);
            }
        }

        private async Task PropagateResultAsync(Tuple<IEnumerable<TOutput1>, IEnumerable<TOutput2>> result)
        {
            await Task.WhenAll(
                result.Item1
                    .Select(item => _output1.SendAsync(item))
                    .Concat(
                        result.Item2
                            .Select(item => _output2.SendAsync(item))));
        }

        private void PropagateCompletion()
        {
            _input.PropagateCompletion(_output1, _output2);
        }
    }
}