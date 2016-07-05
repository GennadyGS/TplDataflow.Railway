using System;

namespace Dataflow.Core
{
    public abstract class Dataflow<T>
    {
        protected Dataflow(IDataflowFactory factory)
        {
            Factory = factory;
        }

        internal IDataflowFactory Factory { get; }

        public abstract DataflowType<T> GetDataflowType();

        public abstract Dataflow<TOutput> Bind<TOutput>(Func<T, Dataflow<TOutput>> bindFunc);
    }
}