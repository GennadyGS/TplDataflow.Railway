using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public class JointPointBlock<T> : ISourceBlock<T>, IReceivableSourceBlock<T>, IDataflowBlock
    {
        private readonly BlockingCollection<Exception> _completionExceptions = new BlockingCollection<Exception>();
        private readonly IPropagatorBlock<T, T> _output = new BufferBlock<T>();
        private int _completedInputCount;

        private int _inputCount;

        bool IReceivableSourceBlock<T>.TryReceive(Predicate<T> filter, out T item)
        {
            return ((IReceivableSourceBlock<T>) _output).TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<T>.TryReceiveAll(out IList<T> items)
        {
            return ((IReceivableSourceBlock<T>) _output).TryReceiveAll(out items);
        }

        public Task Completion
        {
            get
            {
                return _output.Completion;
            }
        }

        void IDataflowBlock.Complete()
        {
            _output.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _output.Fault(exception);
        }

        IDisposable ISourceBlock<T>.LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
        {
            return _output.LinkTo(target, linkOptions);
        }

        T ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target,
            out bool messageConsumed)
        {
            return _output.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<T>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            return _output.ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<T>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            _output.ReleaseReservation(messageHeader, target);
        }

        public ITargetBlock<T> AddInput()
        {
            var result = new BufferBlock<T>();
            result.LinkTo(_output);
            Interlocked.Increment(ref _inputCount);
            result.Completion.ContinueWith(HandleInputCompletion);
            return result;
        }

        private void HandleInputCompletion(Task task)
        {
            if (task.IsFaulted)
            {
                _completionExceptions.Add(task.Exception);
            }
            if (Interlocked.Increment(ref _completedInputCount) >= _inputCount)
            {
                if (_completionExceptions.Count > 0)
                {
                    _output.Fault(new AggregateException(_completionExceptions));
                }
                else
                {
                    _output.Complete();
                }
            }
        }
    }
}