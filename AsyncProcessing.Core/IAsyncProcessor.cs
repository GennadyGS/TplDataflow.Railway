using System;

namespace AsyncProcessing.Core
{
    public interface IAsyncProcessor<in TInput, out TOutput> : IObserver<TInput>, IObservable<TOutput>
    {
    }
}