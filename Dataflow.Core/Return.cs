namespace Dataflow.Core
{
    public class Return<T> : DataflowOperator<T>
    {
        public Return(T result)
        {
            Result = result;
        }

        public T Result { get; }

        public override DataflowType<T> GetDataflowType()
        {
            return new ReturnType<T>();
        }
    }
}