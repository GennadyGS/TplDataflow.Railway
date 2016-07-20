using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AsyncProcessing.Core
{
    public class AsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly Subject<TInput> _input = new Subject<TInput>();
        private readonly Subject<TOutput> _output = new Subject<TOutput>();

        internal AsyncProcessor(Func<IObservable<TInput>, IObservable<TOutput>> dataflowFunc)
        {
            dataflowFunc(_input).Subscribe(_output);
        }

        internal AsyncProcessor(Func<IEnumerable<TInput>, IEnumerable<TOutput>> dataflowFunc)
        {
            _input.ToListAsync()
                .ContinueWith(task =>
                    dataflowFunc(task.Result).Subscribe(_output));
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

    public static class AsyncProcessor
    {
        public static AsyncProcessor<TInput, TOutput> Create<TInput, TOutput>(Func<IEnumerable<TInput>, IEnumerable<TOutput>> dataflowFunc)
        {
            return new AsyncProcessor<TInput, TOutput>(dataflowFunc);
        }

        public static AsyncProcessor<TInput, TOutput> Create<TInput, TOutput>(Func<IObservable<TInput>, IObservable<TOutput>> dataflowFunc)
        {
            return new AsyncProcessor<TInput, TOutput>(dataflowFunc);
        }
    }
}