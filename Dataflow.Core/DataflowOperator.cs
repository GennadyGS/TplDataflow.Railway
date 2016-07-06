using System;

namespace Dataflow.Core
{
    public abstract class DataflowOperator<T> : Dataflow<T>
    {
        protected DataflowOperator(IDataflowFactory factory, IDataflowType<T> type) : base(factory, type)
        {
        }

        public override Dataflow<TOutput> Bind<TOutput>(Func<T, Dataflow<TOutput>> bindFunc)
        {
            return Factory.Calculation(this, bindFunc);
        }
    }
}