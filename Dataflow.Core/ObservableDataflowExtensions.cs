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
            var factory = new ObservableDataflowFactory();
            return input.Select(item => bindFunc(factory, item)).TransformDataflows();
        }

        public static IObservable<TOutput> TransformDataflows<TOutput>(this IObservable<Dataflow<TOutput>> dataflows)
        {
            return dataflows
                .GroupBy(dataflow => dataflow.GetDataflowType())
                .SelectMany(group => group.Key.TransformDataFlows(group));
        }
    }

    public abstract partial class DataflowType<T>
    {
        public abstract IObservable<T> TransformDataFlows(IObservable<Dataflow<T>> dataflows);
    }

    public abstract partial class DataflowOperatorType<T> : DataflowType<T>
    {
        public abstract IObservable<Dataflow<TOutput>> PerformOperator<TOutput>(
            IObservable<DataflowCalculation<T, TOutput>> calculationDataflows);
    }

    public partial class DataflowCalculationType<TInput, TOutput>
    {
        public override IObservable<TOutput> TransformDataFlows(IObservable<Dataflow<TOutput>> dataflows)
        {
            return dataflows
                .Cast<DataflowCalculation<TInput, TOutput>>()
                .GroupBy(dataflow => dataflow.Operator.GetDataflowOperatorType())
                .SelectMany(group => group.Key.PerformOperator(group))
                .TransformDataflows();
        }
    }

    public partial class ReturnType<T>
    {
        public override IObservable<T> TransformDataFlows(IObservable<Dataflow<T>> dataflows)
        {
            return dataflows.Select(dataflow => ((Return<T>)dataflow).Result);
        }

        public override IObservable<Dataflow<TOutput>> PerformOperator<TOutput>(IObservable<DataflowCalculation<T, TOutput>> calculationDataflows)
        {
            return calculationDataflows.Select(dataflow =>
                dataflow.Continuation(((Return<T>)dataflow.Operator).Result));
        }
    }

    public partial class ReturnManyType<T>
    {
        public override IObservable<T> TransformDataFlows(IObservable<Dataflow<T>> dataflows)
        {
            return dataflows.SelectMany(dataflow => ((ReturnMany<T>)dataflow).Result);
        }

        public override IObservable<Dataflow<TOutput>> PerformOperator<TOutput>(IObservable<DataflowCalculation<T, TOutput>> calculationDataflows)
        {
            return calculationDataflows.SelectMany(dataflow =>
                ((ReturnMany<T>)dataflow.Operator).Result.Select(dataflow.Continuation));
        }
    }

    public partial class BufferType<T>
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