namespace Dataflow.Core
{
    public class Return<T> : DataflowOperator<T>
    {
        private static readonly DataflowOperatorType<T> DataflowType = new ReturnType<T>();

        public T Result { get; }

        public Return(T result)
        {
            Result = result;
        }

        public override DataflowOperatorType<T> GetDataflowOperatorType()
        {
            return DataflowType;
        }
    }
}