using System;
using System.Reactive.Subjects;

namespace TplDataFlow.Extensions.AsyncProcessing.Core
{
    public class ObservableAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly Subject<TInput> _input = new Subject<TInput>();
        private readonly Subject<TOutput> _output = new Subject<TOutput>();

        public ObservableAsyncProcessor(Func<IObservable<TInput>, IObservable<TOutput>> dataflow)
        {
            dataflow(_input).Subscribe(_output);
        }

        void IObserver<TInput>.OnNext(TInput value)
        {
            _input.OnNext(value);
        }

        void IObserver<TInput>.OnError(Exception error)
        {
            _input.OnError(error);
        }

        void IObserver<TInput>.OnCompleted()
        {
            _input.OnCompleted();
        }

        IDisposable IObservable<TOutput>.Subscribe(IObserver<TOutput> observer)
        {
            return _output.Subscribe(observer);
        }
    }
}