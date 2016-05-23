using System;

namespace TplDataFlow.Extensions.AsyncProcessing.Core
{
    public interface IAsyncProcessor<in TInput, out TOutput> : IObserver<TInput>, IObservable<TOutput>
    {
    }
}