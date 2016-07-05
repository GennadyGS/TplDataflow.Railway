using System;

namespace Dataflow.Core
{
    public abstract class DataflowOperator<T> : Dataflow<T>
    {
        protected DataflowOperator(IDataflowFactory factory) : base(factory)
        {
        }

        public override DataflowType<T> GetDataflowType()
        {
            return GetDataflowOperatorType();
        }

        public abstract DataflowOperatorType<T> GetDataflowOperatorType();

        public override Dataflow<TOutput> Bind<TOutput>(Func<T, Dataflow<TOutput>> bindFunc)
        {
            return Factory.Calculation(this, bindFunc);
        }
    }
}