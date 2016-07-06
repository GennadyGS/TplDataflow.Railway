using AsyncProcessing.Core;
using Dataflow.Core;
using System;
using System.Reactive.Subjects;

namespace AsyncProcessing.Dataflow
{
    public class DataflowAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly Subject<TInput> _input = new Subject<TInput>();
        private readonly Subject<TOutput> _output = new Subject<TOutput>();

        public DataflowAsyncProcessor(Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataflow)
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