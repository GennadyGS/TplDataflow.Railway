using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Dataflow.Core;

namespace Dataflow.TplDataflow
{
    //public abstract partial class DataflowType<T>
    //{
    //    public abstract ISourceBlock<T> TransformTplDataFlows(ISourceBlock<Dataflow<T>> dataflows);
    //}

    //public abstract partial class DataflowOperatorType<T> : DataflowType<T>
    //{
    //    public abstract ISourceBlock<Dataflow<TOutput>> PerformOperator<TOutput>(
    //        ISourceBlock<DataflowCalculation<T, TOutput>> calculationDataflows);
    //}

    //public partial class DataflowCalculationType<TInput, TOutput>
    //{
    //    public override ISourceBlock<TOutput> TransformTplDataFlows(ISourceBlock<Dataflow<TOutput>> dataflows)
    //    {
    //        return dataflows
    //            .Cast<DataflowCalculation<TInput, TOutput>>()
    //            .GroupBy(dataflow => dataflow.Operator.GetDataflowOperatorType())
    //            .SelectMany(group => group.Key.PerformOperator(group))
    //            .TransformDataflows();
    //    }
    //}

    //public partial class ReturnType<T>
    //{
    //    public override ISourceBlock<T> TransformTplDataFlows(ISourceBlock<Dataflow<T>> dataflows)
    //    {
    //        return dataflows.Select(dataflow => ((Return<T>)dataflow).Result);
    //    }

    //    public override ISourceBlock<Dataflow<TOutput>> PerformOperator<TOutput>(ISourceBlock<DataflowCalculation<T, TOutput>> calculationDataflows)
    //    {
    //        return calculationDataflows.Select(dataflow =>
    //            dataflow.Continuation(((Return<T>)dataflow.Operator).Result));
    //    }
    //}

    //public partial class ReturnManyType<T>
    //{
    //    public override ISourceBlock<T> TransformTplDataFlows(ISourceBlock<Dataflow<T>> dataflows)
    //    {
    //        return dataflows.SelectMany(dataflow => ((ReturnMany<T>)dataflow).Result);
    //    }

    //    public override ISourceBlock<Dataflow<TOutput>> PerformOperator<TOutput>(ISourceBlock<DataflowCalculation<T, TOutput>> calculationDataflows)
    //    {
    //        return calculationDataflows.SelectMany(dataflow =>
    //            ((ReturnMany<T>)dataflow.Operator).Result.Select(dataflow.Continuation));
    //    }
    //}

    //public partial class BufferType<T>
    //{
    //    public override ISourceBlock<IList<T>> TransformTplDataFlows(ISourceBlock<Dataflow<IList<T>>> dataflows)
    //    {
    //        return dataflows
    //            .GroupBy(item => new { ((Buffer<T>)item).BatchMaxSize, ((Buffer<T>)item).BatchTimeout })
    //            .SelectMany(group => group
    //                .Select(item => ((Buffer<T>)item).Item)
    //                .Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize));
    //    }

    //    public override ISourceBlock<Dataflow<TOutput>> PerformOperator<TOutput>(
    //        ISourceBlock<DataflowCalculation<IList<T>, TOutput>> calculationDataflows)
    //    {
    //        return calculationDataflows
    //            .GroupBy(dataflow => new
    //            {
    //                ((Buffer<T>)dataflow.Operator).BatchMaxSize,
    //                ((Buffer<T>)dataflow.Operator).BatchTimeout
    //            })
    //            .SelectMany(group => group.Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize))
    //            .Where(batch => batch.Count > 0)
    //            .Select(batch => new
    //            {
    //                Items = batch.Select(item => ((Buffer<T>)item.Operator).Item).ToList(),
    //                batch.First().Continuation
    //            })
    //            .Select(batch => batch.Continuation(batch.Items));
    //    }
    //}

    internal abstract class TplDataflowType<T> : DataflowType<T>
    {
        public abstract ISourceBlock<T> TransformTplDataFlows(ISourceBlock<Dataflow<T>> dataflows);

        public override IObservable<T> TransformDataFlows(IObservable<Dataflow<T>> dataflows)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> TransformDataFlows(IEnumerable<Dataflow<T>> dataflows)
        {
            throw new NotImplementedException();
        }
    }
}