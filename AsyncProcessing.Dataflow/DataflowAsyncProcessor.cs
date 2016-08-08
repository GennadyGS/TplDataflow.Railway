using AsyncProcessing.Core;
using Dataflow.Core;
using System;
using System.Reactive.Subjects;
using Dataflow.Rx;

namespace AsyncProcessing.Dataflow
{
    public class DataflowAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly Subject<TInput> input = new Subject<TInput>();
        private readonly Subject<TOutput> output = new Subject<TOutput>();

        public DataflowAsyncProcessor(Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataflow)
        {
            input.BindDataflow(dataflow).Subscribe(output);
        }

        void IObserver<TInput>.OnNext(TInput value)
        {
            input.OnNext(value);
        }

        void IObserver<TInput>.OnError(Exception error)
        {
            input.OnError(error);
        }

        void IObserver<TInput>.OnCompleted()
        {
            input.OnCompleted();
        }

        IDisposable IObservable<TOutput>.Subscribe(IObserver<TOutput> observer)
        {
            return output.Subscribe(observer);
        }
    }
}