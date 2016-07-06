using System;

namespace Dataflow.Core
{
    public interface IDataflow<out T>
    {
        IDataflowType<T> Type { get; }

        IDataflowFactory Factory { get; }

        IDataflow<TOutput> Bind<TOutput>(Func<T, IDataflow<TOutput>> bindFunc);
    }
}