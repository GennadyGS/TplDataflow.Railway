using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public static class DataflowBlockFactory
    {
        public static IPropagatorBlock<Result<TSuccess, TFailure>, Result<IList<TSuccess>, TFailure>> CreateTimedBatchBlockSafe<TSuccess, TFailure>(
            TimeSpan batchTimeout, int batchMaxSize)
        {
            var target = new BufferBlock<Result<TSuccess, TFailure>>();
            var source = new BufferBlock<Result<IList<TSuccess>, TFailure>>();

            target.AsObservable()
                .BufferSafe(batchTimeout, batchMaxSize)
                .Where(buffer => !buffer.IsSuccess || buffer.Success.Count > 0)
                .Subscribe(source.AsObserver());

            return DataflowBlock.Encapsulate(target, source);
        }

        public static IPropagatorBlock<T, T> CreateSideEffectBlock<T>(Action<T> sideEffectAction)
        {
            return new TransformBlock<T, T>(arg =>
            {
                sideEffectAction(arg);
                return arg;
            });
        }

        public static IPropagatorBlock<T, T> CreateSideEffectBlock<T>(Func<T, Task> sideEffectAction)
        {
            return new TransformBlock<T, T>(async arg =>
            {
                await sideEffectAction(arg);
                return arg;
            });
        }
    }
}