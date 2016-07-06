using System;

namespace Dataflow.Core
{
    public abstract class Dataflow<T> : IDataflow<T>
    {
        protected Dataflow(IDataflowFactory factory, IDataflowType<T> type)
        {
            Factory = factory;
            Type = type;
        }

        public IDataflowFactory Factory { get; }

        public IDataflowType<T> Type { get; }

        public abstract IDataflow<TOutput> Bind<TOutput>(Func<T, IDataflow<TOutput>> bindFunc);
    }
}