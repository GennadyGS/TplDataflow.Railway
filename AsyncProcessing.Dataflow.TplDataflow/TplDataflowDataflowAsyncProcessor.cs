using AsyncProcessing.Core;
using Dataflow.Core;
using System;
using System.Threading.Tasks.Dataflow;
using Dataflow.TplDataflow;
using TplDataFlow.Extensions;

namespace AsyncProcessing.Dataflow.TplDataflow
{
    public class TplDataflowDataflowAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly BufferBlock<TInput> _inputBlock = new BufferBlock<TInput>();
        private readonly BufferBlock<TOutput> _outputBlock = new BufferBlock<TOutput>();

        public TplDataflowDataflowAsyncProcessor(Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataflow)
        {
            _inputBlock.BindDataflow(dataflow).LinkWith(_outputBlock);
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