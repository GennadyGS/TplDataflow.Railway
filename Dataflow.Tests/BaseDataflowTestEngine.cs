using System;
using System.Collections.Generic;
using Dataflow.Core;
using FluentAssertions;

namespace Dataflow.Tests
{
    abstract class BaseDataflowTestEngine : IDataflowTestEngine
    {
        void IDataflowTestEngine.TestBindDataflow<TInput, TOutput>(IEnumerable<TOutput> expectedOutput, IEnumerable<TInput> input, 
            Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataFlowFunc)
        {
            IEnumerable<TOutput> enumerable = TransformInput(input, dataFlowFunc);
            enumerable.ShouldAllBeEquivalentTo(expectedOutput, "Enumerable result should be correct");
        }

        protected abstract IEnumerable<TOutput> TransformInput<TInput, TOutput>(IEnumerable<TInput> input, Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataflowBindFunc);
    }
}