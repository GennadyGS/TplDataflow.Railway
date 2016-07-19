using System;
using System.Collections.Generic;
using Dataflow.Core;

namespace Dataflow.Tests
{
    public interface IDataflowTestEngine
    {
        void TestBindDataflow<TInput, TOutput>(IEnumerable<TOutput> expectedOutput, IEnumerable<TInput> input, 
            Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataFlowFunc);
    }
}