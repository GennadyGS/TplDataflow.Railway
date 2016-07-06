﻿using Dataflow.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using TplDataflow.Linq;

namespace Dataflow.TplDataflow
{
    public static class TplDataflowDataFlowExtensions
    {
        public static ISourceBlock<TOutput> BindDataflow<TInput, TOutput>(this ISourceBlock<TInput> input,
            Func<IDataflowFactory, TInput, IDataflow<TOutput>> bindFunc)
        {
            var factory = new DataflowFactory(new DataflowTypeFactory());
            return input.Select(item => bindFunc(factory, item)).TransformDataflows();
        }

        private static ISourceBlock<TOutput> TransformDataflows<TOutput>(this ISourceBlock<IDataflow<TOutput>> dataflows)
        {
            return dataflows
                .GroupBy(dataflow => dataflow.Type)
                .SelectMany(group => ((DataflowType<TOutput>)group.Key).TransformDataFlows(group));
        }

        private class DataflowTypeFactory : IDataflowTypeFactory
        {
            IDataflowType<TOutput> IDataflowTypeFactory.CreateCalculationType<TInput, TOutput>()
            {
                return new DataflowCalculationType<TInput, TOutput>();
            }

            IDataflowType<T> IDataflowTypeFactory.CreateReturnType<T>()
            {
                return new ReturnType<T>();
            }

            IDataflowType<T> IDataflowTypeFactory.CreateReturnManyType<T>()
            {
                return new ReturnManyType<T>();
            }

            public IDataflowType<IList<T>> CreateBufferType<T>()
            {
                return new BufferType<T>();
            }
        }

        private abstract class DataflowType<T> : IDataflowType<T>
        {
            public abstract ISourceBlock<T> TransformDataFlows(ISourceBlock<IDataflow<T>> dataflows);
        }

        private abstract class DataflowOperatorType<T> : DataflowType<T>
        {
            public abstract ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<T, TOutput>> calculationDataflows);

        }

        private class DataflowCalculationType<TInput, TOutput> : DataflowType<TOutput>
        {
            public override ISourceBlock<TOutput> TransformDataFlows(ISourceBlock<IDataflow<TOutput>> dataflows)
            {
                return dataflows
                    .Cast<DataflowCalculation<TInput, TOutput>>()
                    .GroupBy(dataflow => dataflow.Operator.Type)
                    .SelectMany(group => ((DataflowOperatorType<TInput>)group.Key).PerformOperator(group))
                    .TransformDataflows();
            }
        }

        private class ReturnType<T> : DataflowOperatorType<T>
        {
            public override ISourceBlock<T> TransformDataFlows(ISourceBlock<IDataflow<T>> dataflows)
            {
                return dataflows.Select(dataflow => ((Return<T>)dataflow).Result);
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<T, TOutput>> calculationDataflows)
            {
                return calculationDataflows.Select(dataflow =>
                    dataflow.Continuation(((Return<T>)dataflow.Operator).Result));
            }
        }

        private class ReturnManyType<T> : DataflowOperatorType<T>
        {
            public override ISourceBlock<T> TransformDataFlows(ISourceBlock<IDataflow<T>> dataflows)
            {
                return dataflows.SelectMany(dataflow => ((ReturnMany<T>)dataflow).Result);
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<T, TOutput>> calculationDataflows)
            {
                return calculationDataflows.SelectMany(dataflow =>
                    ((ReturnMany<T>)dataflow.Operator).Result.Select(dataflow.Continuation));
            }
        }

        private class BufferType<T> : DataflowOperatorType<IList<T>>
        {
            public override ISourceBlock<IList<T>> TransformDataFlows(ISourceBlock<IDataflow<IList<T>>> dataflows)
            {
                return dataflows
                    .GroupBy(item => new { ((Buffer<T>)item).BatchMaxSize, ((Buffer<T>)item).BatchTimeout })
                    .SelectMany(group => group
                        .Select(item => ((Buffer<T>)item).Item)
                        .Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize));
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<IList<T>, TOutput>> calculationDataflows)
            {
                return calculationDataflows
                    .GroupBy(dataflow => new
                    {
                        ((Buffer<T>)dataflow.Operator).BatchMaxSize,
                        ((Buffer<T>)dataflow.Operator).BatchTimeout
                    })
                    .SelectMany(group => group.Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize))
                    .Select(batch => new
                    {
                        Items = batch.Select(item => ((Buffer<T>)item.Operator).Item).ToList(),
                        batch.First().Continuation
                    })
                    .Select(batch => batch.Continuation(batch.Items));
            }
        }
    }
}