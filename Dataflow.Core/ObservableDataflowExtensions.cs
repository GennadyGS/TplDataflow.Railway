using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Dataflow.Core
{
    public static class ObservableDataFlowExtensions
    {
        public static IObservable<TOutput> BindDataflow<TInput, TOutput>(this IObservable<TInput> input,
            Func<IDataflowFactory, TInput, Dataflow<TOutput>> bindFunc)
        {
            var factory = new DataflowFactory(new DataflowTypeFactory());
            return input.Select(item => bindFunc(factory, item)).TransformDataflows();
        }

        private static IObservable<TOutput> TransformDataflows<TOutput>(this IObservable<Dataflow<TOutput>> dataflows)
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
            public abstract IObservable<T> TransformDataFlows(IObservable<Dataflow<T>> dataflows);
        }

        private abstract class DataflowOperatorType<T> : DataflowType<T>
        {
            public abstract IObservable<Dataflow<TOutput>> PerformOperator<TOutput>(
                IObservable<DataflowCalculation<T, TOutput>> calculationDataflows);

        }

        private class DataflowCalculationType<TInput, TOutput> : DataflowType<TOutput>
        {
            public override IObservable<TOutput> TransformDataFlows(IObservable<Dataflow<TOutput>> dataflows)
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
            public override IObservable<T> TransformDataFlows(IObservable<Dataflow<T>> dataflows)
            {
                return dataflows.Select(dataflow => ((Return<T>)dataflow).Result);
            }

            public override IObservable<Dataflow<TOutput>> PerformOperator<TOutput>(
                IObservable<DataflowCalculation<T, TOutput>> calculationDataflows)
            {
                return calculationDataflows.Select(dataflow =>
                    dataflow.Continuation(((Return<T>)dataflow.Operator).Result));
            }
        }

        private class ReturnManyType<T> : DataflowOperatorType<T>
        {
            public override IObservable<T> TransformDataFlows(IObservable<Dataflow<T>> dataflows)
            {
                return dataflows.SelectMany(dataflow => ((ReturnMany<T>)dataflow).Result);
            }

            public override IObservable<Dataflow<TOutput>> PerformOperator<TOutput>(
                IObservable<DataflowCalculation<T, TOutput>> calculationDataflows)
            {
                return calculationDataflows.SelectMany(dataflow =>
                    ((ReturnMany<T>)dataflow.Operator).Result.Select(dataflow.Continuation));
            }
        }

        private class BufferType<T> : DataflowOperatorType<IList<T>>
        {
            public override IObservable<IList<T>> TransformDataFlows(IObservable<Dataflow<IList<T>>> dataflows)
            {
                return dataflows
                    .GroupBy(item => new { ((Buffer<T>)item).BatchMaxSize, ((Buffer<T>)item).BatchTimeout })
                    .SelectMany(group => group
                        .Select(item => ((Buffer<T>)item).Item)
                        .Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize));
            }

            public override IObservable<Dataflow<TOutput>> PerformOperator<TOutput>(
                IObservable<DataflowCalculation<IList<T>, TOutput>> calculationDataflows)
            {
                return calculationDataflows
                    .GroupBy(dataflow => new
                    {
                        ((Buffer<T>)dataflow.Operator).BatchMaxSize,
                        ((Buffer<T>)dataflow.Operator).BatchTimeout
                    })
                    .SelectMany(group => group.Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize))
                    .Where(batch => batch.Count > 0)
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