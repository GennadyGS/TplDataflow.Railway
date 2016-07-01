using System;
using System.Collections.Generic;
using System.Linq;
using Railway.Linq;

namespace Dataflow.Core
{
    public class Buffer<T> : DataflowOperator<IList<T>>
    {
        public Buffer(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            Item = item;
            BatchTimeout = batchTimeout;
            BatchMaxSize = batchMaxSize;
        }

        public T Item { get; }

        public int BatchMaxSize { get; }

        public TimeSpan BatchTimeout { get; }

        public override IEnumerable<IList<T>> TransformEnumerableOfDataFlow(IEnumerable<Dataflow<IList<T>>> dataflows)
        {
            return dataflows
                .Cast<Buffer<T>>()
                .Select(item => item.Item)
                .Buffer(BatchMaxSize);
        }

        public override IEnumerable<Dataflow<TOutput>> TransformEnumerableOfCalculationDataFlow<TOutput>(IEnumerable<DataflowCalculation<IList<T>, TOutput>> calculationDataflows)
        {
            return calculationDataflows
                .Buffer(BatchMaxSize)
                .Where(batch => batch.Count > 0)
                .Select(batch =>
                {
                    List<T> items = batch
                        .Select(item => ((Buffer<T>)item.Operator).Item)
                        .ToList();
                    return batch.First().Continuation(items);
                });
        }
    }
}