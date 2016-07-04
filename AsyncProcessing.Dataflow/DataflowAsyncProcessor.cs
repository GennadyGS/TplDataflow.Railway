using System;
using System.Reactive.Subjects;
using AsyncProcessing.Core;
using Dataflow.Core;

namespace AsyncProcessing.Dataflow
{
    public class DataflowAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly Subject<TInput> _input = new Subject<TInput>();
        private readonly Subject<TOutput> _output = new Subject<TOutput>();

        public DataflowAsyncProcessor(Func<TInput, Dataflow<TOutput>> dataflow)
        {
            _input.BindDataflow(dataflow).Subscribe(_output);
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