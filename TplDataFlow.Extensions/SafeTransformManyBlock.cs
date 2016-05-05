﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TplDataFlow.Extensions
{
    public class SafeTransformManyBlock<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>, IReceivableSourceBlock<TOutput>
    {
        private readonly ITargetBlock<TInput> _transformActionBlock;

        private readonly BufferBlock<TOutput> _outputBufferBlock = new BufferBlock<TOutput>();
        private readonly BufferBlock<Tuple<Exception, TInput>> _exceptionBufferBlock = new BufferBlock<Tuple<Exception, TInput>>();

        public SafeTransformManyBlock(Func<TInput, Task<IEnumerable<TOutput>>> transform)
        {
            _transformActionBlock = new ActionBlock<TInput>(CreateTransformActionAsync(transform));

            PropagateCompletion();
        }

        public SafeTransformManyBlock(Func<TInput, IEnumerable<TOutput>> transform)
        {
            _transformActionBlock = new ActionBlock<TInput>(CreateTransformActionSync(transform));

            PropagateCompletion();
        }

        public ISourceBlock<Tuple<Exception, TInput>> Exception
        {
            get
            {
                return _exceptionBufferBlock;
            }
        }

        public SafeTransformManyBlock<TInput, TOutput> HandleExceptionWith(ITargetBlock<Tuple<Exception, TInput>> exceptionHandler)
        {
            Exception.LinkWith(exceptionHandler);
            return this;
        }

        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, bool consumeToAccept)
        {
            return _transformActionBlock.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        void IDataflowBlock.Complete()
        {
            _transformActionBlock.Complete();
        }

        void IDataflowBlock.Fault(Exception exception)
        {
            _transformActionBlock.Fault(exception);
        }

        Task IDataflowBlock.Completion
        {
            get
            {
                return Task.WhenAll(_outputBufferBlock.Completion, _exceptionBufferBlock.Completion);
            }
        }

        IDisposable ISourceBlock<TOutput>.LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            return _outputBufferBlock.LinkTo(target, linkOptions);
        }

        TOutput ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        {
            return ((ISourceBlock<TOutput>)_outputBufferBlock).ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            return ((ISourceBlock<TOutput>)_outputBufferBlock).ReserveMessage(messageHeader, target);
        }

        void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            ((ISourceBlock<TOutput>)_outputBufferBlock).ReleaseReservation(messageHeader, target);
        }

        bool IReceivableSourceBlock<TOutput>.TryReceive(Predicate<TOutput> filter, out TOutput item)
        {
            return _outputBufferBlock.TryReceive(filter, out item);
        }

        bool IReceivableSourceBlock<TOutput>.TryReceiveAll(out IList<TOutput> items)
        {
            return _outputBufferBlock.TryReceiveAll(out items);
        }

        private Action<TInput> CreateTransformActionSync(Func<TInput, IEnumerable<TOutput>> transform)
        {
            return input =>
            {
                try
                {
                    Exception exception = null;
                    IEnumerable<TOutput> output = Enumerable.Empty<TOutput>();
                    try
                    {
                        output = transform(input);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }

                    if (exception == null)
                    {
                        foreach(var item in output)
                        {
                            _outputBufferBlock.Post(item);
                        }
                    }
                    else
                    {
                        _exceptionBufferBlock.Post(new Tuple<Exception, TInput>(exception, input));
                    }
                }
                catch (Exception e)
                {
                    _exceptionBufferBlock.Post(new Tuple<Exception, TInput>(e, input));
                }
            };
        }

        private Func<TInput, Task> CreateTransformActionAsync(Func<TInput, Task<IEnumerable<TOutput>>> transform)
        {
            return async input =>
            {
                Exception exception = null;
                IEnumerable<TOutput> output = Enumerable.Empty<TOutput>();
                try
                {
                    output = await transform(input);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                if (exception == null)
                {
                    foreach (var item in output)
                    {
                        await _outputBufferBlock.SendAsync(item);
                    }
                }
                else
                {
                    await _exceptionBufferBlock.SendAsync(new Tuple<Exception, TInput>(exception, input));
                }
            };
        }

        private void PropagateCompletion()
        {
            _transformActionBlock.PropagateCompletion(_outputBufferBlock, _exceptionBufferBlock);
        }
    }
}