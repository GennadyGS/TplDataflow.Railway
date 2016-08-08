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
        private readonly BlockingCollection<Exception> completionExceptions = new BlockingCollection<Exception>();
        private readonly IPropagatorBlock<T, T> output = new BufferBlock<T>();
        private int completedInputCount;

        private int inputCount;

        bool IReceivableSourceBlock<T>.TryReceive(Predicate<T> filter, out T item)
        {
            return ((IReceivableSourceBlock<T>)output).TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<T>.TryReceiveAll(out IList<T> items)
        {
            return ((IReceivableSourceBlock<T>)output).TryReceiveAll(out items);
        }

        public Task Completion
        {
            get
            {
                return output.Completion;
            }
        }

        void IDataflowBlock.Complete()
        {
            output.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            output.Fault(exception);
        }

        IDisposable ISourceBlock<T>.LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
        {
            return output.LinkTo(target, linkOptions);
        }

        T ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target,
            out bool messageConsumed)
        {
            return output.ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<T>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            return output.ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<T>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
        {
            output.ReleaseReservation(messageHeader, target);
        }

        public ITargetBlock<T> AddInput()
        {
            var result = new BufferBlock<T>();
            result.LinkTo(output);
            Interlocked.Increment(ref inputCount);
            result.Completion.ContinueWith(HandleInputCompletion);
            return result;
        }

        private void HandleInputCompletion(Task task)
        {
            if (task.IsFaulted)
            {
                completionExceptions.Add(task.Exception);
            }
            if (Interlocked.Increment(ref completedInputCount) >= inputCount)
            {
                if (completionExceptions.Count > 0)
                {
                    output.Fault(new AggregateException(completionExceptions));
                }
                else
                {
                    output.Complete();
                }
            }
        }
    }
}