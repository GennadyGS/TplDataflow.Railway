using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions.AsyncProcessing
{
    public abstract class TplDataflowAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly BufferBlock<TInput> _inputBlock = new BufferBlock<TInput>();
        private readonly BufferBlock<TOutput> _outputBlock = new BufferBlock<TOutput>();

        protected TplDataflowAsyncProcessor()
        {
        }

        private IObserver<TInput> InputObserver => _inputBlock.AsObserver();

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
            return _outputBlock.AsObservable().Subscribe(observer);
        }

        protected abstract ISourceBlock<TOutput> CreateDataflow(ISourceBlock<TInput> input);

        // TODO: Get rid of it
        protected void InitializeDataflow()
        {
            CreateDataflow(_inputBlock).LinkWith(_outputBlock);
        }
    }
}