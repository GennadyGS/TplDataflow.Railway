using System;

namespace Dataflow.Core
{
    public abstract class Dataflow<T>
    {
        protected Dataflow(IDataflowFactory factory, IDataflowType<T> type)
        {
            Factory = factory;
            Type = type;
        }

        internal IDataflowFactory Factory { get; }

        public IDataflowType<T> Type { get; }

        public abstract Dataflow<TOutput> Bind<TOutput>(Func<T, Dataflow<TOutput>> bindFunc);
    }
}