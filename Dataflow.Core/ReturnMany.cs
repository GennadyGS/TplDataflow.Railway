using System.Collections.Generic;

namespace Dataflow.Core
{
    public class ReturnMany<T> : DataflowOperator<T>
    {
        private static readonly DataflowOperatorType<T> DataflowType = new ReturnManyType<T>();

        public IEnumerable<T> Result { get; }

        public ReturnMany(IEnumerable<T> result)
        {
            Result = result;
        }

        public override DataflowOperatorType<T> GetDataflowOperatorType()
        {
            return DataflowType;
        }
    }
}