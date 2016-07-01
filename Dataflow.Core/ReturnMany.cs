using System.Collections.Generic;

namespace Dataflow.Core
{
    public class ReturnMany<T> : DataflowOperator<T>
    {
        public ReturnMany(IEnumerable<T> result)
        {
            Result = result;
        }

        public IEnumerable<T> Result { get; }

        public override DataflowType<T> GetDataflowType()
        {
            return new ReturnManyType<T>();
        }
    }
}