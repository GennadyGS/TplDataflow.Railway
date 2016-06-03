using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AsyncProcessing.Core
{
    public class AsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly Subject<TInput> _input = new Subject<TInput>();
        private readonly Subject<TOutput> _output = new Subject<TOutput>();

        public AsyncProcessor(Func<IObservable<TInput>, IObservable<TOutput>> dataflow)
        {
            dataflow(_input).Subscribe(_output);
        }

        public AsyncProcessor(Func<TInput, IObservable<TOutput>> transform)
        {
            _input.SelectMany(transform).Subscribe(_output);
        }


        public AsyncProcessor(Func<IEnumerable<TInput>, IEnumerable<TOutput>> dataflow)
        {
            _input.ToListAsync()
                .ContinueWith(task =>
                    dataflow(task.Result).Subscribe(_output));
        }

        public AsyncProcessor(Func<TInput, IEnumerable<TOutput>> transform)
        {
            _input.ToListAsync()
                .ContinueWith(task =>
                    task.Result.SelectMany(transform).Subscribe(_output));
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