using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace AsyncProcessing.Core
{
    public class AsyncProcessor<TInput, TOutput> : IAsyncProcessor<TInput, TOutput>
    {
        private readonly Subject<TInput> input = new Subject<TInput>();
        private readonly Subject<TOutput> output = new Subject<TOutput>();

        internal AsyncProcessor(Func<IObservable<TInput>, IObservable<TOutput>> dataflowFunc)
        {
            dataflowFunc(input)
                .Subscribe(output);
        }

        internal AsyncProcessor(Func<IEnumerable<TInput>, IEnumerable<TOutput>> dataflowFunc)
        {
            input.ToListAsync()
                .ContinueWith(task =>
                    dataflowFunc(task.Result).Subscribe(output));
        }

        internal AsyncProcessor(Func<IEnumerable<TInput>, Task<IEnumerable<TOutput>>> dataflowFunc)
        {
            input.ToListAsync()
                .ContinueWith(async task =>
                    (await dataflowFunc(task.Result)).Subscribe(output));
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

    public static class AsyncProcessor
    {
        public static AsyncProcessor<TInput, TOutput> Create<TInput, TOutput>(Func<IEnumerable<TInput>, IEnumerable<TOutput>> dataflowFunc)
        {
            return new AsyncProcessor<TInput, TOutput>(dataflowFunc);
        }

        public static AsyncProcessor<TInput, TOutput> Create<TInput, TOutput>(Func<IEnumerable<TInput>, Task<IEnumerable<TOutput>>> dataflowFunc)
        {
            return new AsyncProcessor<TInput, TOutput>(dataflowFunc);
        }

        public static AsyncProcessor<TInput, TOutput> Create<TInput, TOutput>(Func<IObservable<TInput>, IObservable<TOutput>> dataflowFunc)
        {
            return new AsyncProcessor<TInput, TOutput>(dataflowFunc);
        }
    }
}