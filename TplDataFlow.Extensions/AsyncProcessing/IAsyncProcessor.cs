using System;

namespace TplDataFlow.Extensions.AsyncProcessing
{
    public interface IAsyncProcessor<in TInput, out TOutput> : IObserver<TInput>, IObservable<TOutput>
    {
    }
}