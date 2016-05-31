using System;
using System.Threading.Tasks.Dataflow;
using AsyncProcessing.Core;
using TplDataFlow.Extensions.TplDataflow.Extensions;

namespace TplDataFlow.Extensions.AsyncProcessing.TplDataflow
{
    public class TplDataflowAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly BufferBlock<TInput> _inputBlock = new BufferBlock<TInput>();
        private readonly BufferBlock<TOutput> _outputBlock = new BufferBlock<TOutput>();

        public TplDataflowAsyncProcessor(Func<ISourceBlock<TInput>, ISourceBlock<TOutput>> dataflow)
        {
            dataflow(_inputBlock).LinkWith(_outputBlock);
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
    }
}