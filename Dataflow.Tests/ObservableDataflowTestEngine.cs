using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Dataflow.Core;
using Dataflow.Rx;

namespace Dataflow.Tests
{
    internal class ObservableDataflowTestEngine : BaseDataflowTestEngine
    {
        protected override IEnumerable<TOutput> TransformInput<TInput, TOutput>(IEnumerable<TInput> input, Func<IDataflowFactory, TInput,
            IDataflow<TOutput>> dataflowBindFunc)
        {
            return input
                .ToObservable()
                .BindDataflow(dataflowBindFunc)
                .ToEnumerable();
        }
    }
}