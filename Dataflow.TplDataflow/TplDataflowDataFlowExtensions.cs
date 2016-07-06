using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks.Dataflow;
using Dataflow.Core;
using TplDataflow.Linq;

namespace Dataflow.TplDataflow
{
    public static class TplDataflowDataFlowExtensions
    {
        public static ISourceBlock<TOutput> BindDataflow<TInput, TOutput>(this ISourceBlock<TInput> input,
            Func<IDataflowFactory, TInput, Dataflow<TOutput>> bindFunc)
        {
            var dataflowFactory = new TplDataflowDataflowFactory();
            return input
                .Select(item => bindFunc(dataflowFactory, item))
                .TransformDataflows();
        }

        private static ISourceBlock<TOutput> TransformDataflows<TOutput>(this ISourceBlock<Dataflow<TOutput>> dataflows)
        {
            throw new NotImplementedException();
        }
    }
}