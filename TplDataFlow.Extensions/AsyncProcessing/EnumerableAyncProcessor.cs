using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace TplDataFlow.Extensions.AsyncProcessing
{
    public abstract class EnumerableAsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly Subject<TInput> _input = new Subject<TInput>();
        private readonly Subject<TOutput> _output = new Subject<TOutput>();

        protected EnumerableAsyncProcessor()
        {
            Task.Run(() =>
            {
                CreateDataflow(_input.ToEnumerable().ToList())
                    .Subscribe(_output);
            });
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

        protected abstract IEnumerable<TOutput> CreateDataflow(IEnumerable<TInput> input);
    }
}