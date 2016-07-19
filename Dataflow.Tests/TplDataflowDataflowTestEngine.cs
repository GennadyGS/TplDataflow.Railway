using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using Dataflow.Core;
using Dataflow.TplDataflow;

namespace Dataflow.Tests
{
    internal class TplDataflowDataflowTestEngine : BaseDataflowTestEngine
    {
        protected override IEnumerable<TOutput> TransformInput<TInput, TOutput>(IEnumerable<TInput> input, Func<IDataflowFactory, TInput,
            IDataflow<TOutput>> dataflowBindFunc)
        {
            var block = new BufferBlock<TInput>();
            input.ToObservable().Subscribe(block.AsObserver());
            return block
                .BindDataflow(dataflowBindFunc)
                .AsObservable()
                .ToEnumerable();
        }
    }
}