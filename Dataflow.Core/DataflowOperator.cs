using System;
using System.Collections.Generic;

namespace Dataflow.Core
{
    public abstract class DataflowOperator<T> : Dataflow<T>
    {
        public override Dataflow<TOutput> Bind<TOutput>(Func<T, Dataflow<TOutput>> bindFunc)
        {
            return Dataflow.Calculation(this, bindFunc);
        }

        public abstract IEnumerable<Dataflow<TOutput>> TransformEnumerableOfCalculationDataFlow<TOutput>(IEnumerable<DataflowCalculation<T, TOutput>> calculationDataflows);
    }
}