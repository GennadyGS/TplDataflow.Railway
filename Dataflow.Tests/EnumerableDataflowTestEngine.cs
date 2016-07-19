using System;
using System.Collections.Generic;
using Dataflow.Core;

namespace Dataflow.Tests
{
    internal class EnumerableDataflowTestEngine : BaseDataflowTestEngine
    {
        protected override IEnumerable<TOutput> TransformInput<TInput, TOutput>(IEnumerable<TInput> input, Func<IDataflowFactory, TInput, 
            IDataflow<TOutput>> dataflowBindFunc)
        {
            return input.BindDataflow(dataflowBindFunc);
        }
    }
}