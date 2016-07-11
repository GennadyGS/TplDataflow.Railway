using System;

namespace Dataflow.Core
{
    public interface IDataflowType<out T>
    {
        Type TypeOfDataflow { get; }
    }
}