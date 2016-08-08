using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dataflow.Core
{
    public class ReturnManyAsync<T> : DataflowOperator<T, ReturnManyAsync<T>>
    {
        public Task<IEnumerable<T>> Result { get; }

        public ReturnManyAsync(IDataflowFactory factory, IDataflowType<T> type, Task<IEnumerable<T>> result) 
            : base(factory, type)
        {
            Result = result;
        }
    }
}