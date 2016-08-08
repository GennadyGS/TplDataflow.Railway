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
        private readonly BufferBlock<TInput> inputBlock = new BufferBlock<TInput>();
        private readonly BufferBlock<TOutput> outputBlock = new BufferBlock<TOutput>();

        public TplDataflowDataflowAsyncProcessor(Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataflow)
        {
            inputBlock.BindDataflow(dataflow).LinkWith(outputBlock);
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