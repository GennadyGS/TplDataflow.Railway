namespace Dataflow.Core
{
    public class Return<T> : DataflowOperator<T>
    {
        public T Result { get; }

        public Return(IDataflowFactory dataflowFactory, IDataflowType<T> dataflowType, T result) 
            : base(dataflowFactory, dataflowType)
        {
            Result = result;
        }
    }
}