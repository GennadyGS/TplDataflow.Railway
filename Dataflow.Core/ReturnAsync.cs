using System.Threading.Tasks;

namespace Dataflow.Core
{
    internal class ReturnAsync<T> : DataflowOperator<T, ReturnAsync<T>>
    {
        public Task<T> Result { get; }

        public ReturnAsync(DataflowFactory dataflowFactory, IDataflowType<T> dataflowType, Task<T> result)
            : base(dataflowFactory, dataflowType)
        {
            Result = result;
        }
    }
}