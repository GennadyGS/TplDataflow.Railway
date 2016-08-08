using AsyncProcessing.Core;
using System;
using System.Threading.Tasks.Dataflow;
using TplDataFlow.Extensions;

namespace AsyncProcessing.TplDataflow
{
    public class TplDataflowAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly BufferBlock<TInput> inputBlock = new BufferBlock<TInput>();
        private readonly BufferBlock<TOutput> outputBlock = new BufferBlock<TOutput>();

        public TplDataflowAsyncProcessor(Func<ISourceBlock<TInput>, ISourceBlock<TOutput>> dataflow)
        {
            dataflow(inputBlock).LinkWith(outputBlock);
        }

        private IObserver<TInput> InputObserver => inputBlock.AsObserver();

        void IObserver<TInput>.OnNext(TInput value)
        {
            InputObserver.OnNext(value);
        }

        void IObserver<TInput>.OnError(Exception error)
        {
            InputObserver.OnError(error);
        }

        void IObserver<TInput>.OnCompleted()
        {
            InputObserver.OnCompleted();
        }

        IDisposable IObservable<TOutput>.Subscribe(IObserver<TOutput> observer)
        {
            return outputBlock.AsObservable().Subscribe(observer);
        }
    }
}