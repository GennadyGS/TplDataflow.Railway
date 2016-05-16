using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public static class DataflowBlockFactory
    {
        public static IPropagatorBlock<T, T[]> CreateTimedBatchBlock<T>(int batchMaxSize, TimeSpan batchTimeout)
        {
            var target = new BufferBlock<T>();
            var source = new BufferBlock<T[]>();

            target.AsObservable()
                .Buffer(batchTimeout, batchMaxSize)
                .Where(buffer => buffer.Count > 0)
                .Select(list => list.ToArray())
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