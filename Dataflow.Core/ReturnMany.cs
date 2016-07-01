using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public class ReturnMany<T> : DataflowOperator<T>
    {
        public ReturnMany(IEnumerable<T> result)
        {
            Result = result;
        }

        public IEnumerable<T> Result { get; }

        public override IEnumerable<T> TransformEnumerableOfDataFlow(IEnumerable<Dataflow<T>> dataflows)
        {
            return dataflows.Cast<ReturnMany<T>>().SelectMany(dataflow => dataflow.Result);
        }

        public override IEnumerable<Dataflow<TOutput>> TransformEnumerableOfCalculationDataFlow<TOutput>(IEnumerable<DataflowCalculation<T, TOutput>> calculationDataflows)
        {
            return calculationDataflows.SelectMany(dataflow =>
                ((ReturnMany<T>)dataflow.Operator).Result.Select(dataflow.Continuation));
        }
    }
}