using System.Collections.Generic;

namespace Dataflow.Core
{
    public class ReturnMany<T> : DataflowOperator<T>
    {
        public IEnumerable<T> Result { get; }

        public ReturnMany(IDataflowFactory factory, IDataflowType<T> type, IEnumerable<T> result) 
            : base(factory, type)
        {
            Result = result;
        }
    }
}