using System;

namespace Dataflow.Core
{
    public abstract class DataflowOperator<T, TDataflowOperator> : Dataflow<T> where TDataflowOperator: DataflowOperator<T, TDataflowOperator>
    {
        protected DataflowOperator(IDataflowFactory factory, IDataflowType<T> type) : base(factory, type)
        {
        }

        public override IDataflow<TOutput> Bind<TOutput>(Func<T, IDataflow<TOutput>> bindFunc)
        {
            return Factory.Calculation((TDataflowOperator)this, bindFunc);
        }
    }
}