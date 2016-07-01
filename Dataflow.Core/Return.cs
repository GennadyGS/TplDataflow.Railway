using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public class Return<T> : DataflowOperator<T>
    {
        public Return(T result)
        {
            Result = result;
        }

        public T Result { get; }

        public override IEnumerable<T> TransformEnumerableOfDataFlow(IEnumerable<Dataflow<T>> dataflows)
        {
            return dataflows.Cast<Return<T>>().Select(dataflow => dataflow.Result);
        }

        public override IEnumerable<Dataflow<TOutput>> TransformEnumerableOfCalculationDataFlow<TOutput>(IEnumerable<DataflowCalculation<T, TOutput>> calculationDataflows)
        {
            return calculationDataflows.Select(dataflow =>
                dataflow.Continuation(((Return<T>)dataflow.Operator).Result));
        }
    }
}