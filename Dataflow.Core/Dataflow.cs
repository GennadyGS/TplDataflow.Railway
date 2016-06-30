﻿using System;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;

namespace Dataflow.Core
{
    public abstract class Dataflow<T>
    {
    }

    public abstract class DataflowOperator<T> : Dataflow<T>
    {
    }

    public class Return<T> : DataflowOperator<T>
    {
        public Return(T result)
        {
            Result = result;
        }

        public T Result { get; }
    }

    public class ReturnMany<T> : DataflowOperator<T>
    {
        public ReturnMany(IEnumerable<T> result)
        {
            Result = result;
        }

        public IEnumerable<T> Result { get; }
    }

    public class Buffer<T> : DataflowOperator<T>
    {
        public Buffer(T item, TimeSpan batchTimeout, int batchMaxSize)
        {
            Item = item;
            BatchTimeout = batchTimeout;
            BatchMaxSize = batchMaxSize;
        }

        public T Item { get; }

        public int BatchMaxSize { get; }

        public TimeSpan BatchTimeout { get; }
    }

    public class DataflowCalculation<TInput, TOutput> : Dataflow<TOutput>
    {
        public DataflowCalculation(DataflowOperator<TInput> @operator, Func<TInput, Dataflow<TOutput>> continuation)
        {
            Operator = @operator;
            Continuation = continuation;
        }

        public DataflowOperator<TInput> Operator { get; }

        public Func<TInput, Dataflow<TOutput>> Continuation { get; }
    }

    public class Continuation<TOutput> : Dataflow<TOutput>
    {
        public Func<Dataflow<TOutput>> Func { get; }

        public Continuation(Func<Dataflow<TOutput>> func)
        {
            Func = func;
        }
    }

    public class ContinuationMany<TOutput> : Dataflow<TOutput>
    {
        public Func<IEnumerable<Dataflow<TOutput>>> Func { get; }

        public ContinuationMany(Func<IEnumerable<Dataflow<TOutput>>> func)
        {
            Func = func;
        }
    }

    public class BufferContinuation<TOutput>: Dataflow<TOutput>
    {
        public Func<Dataflow<TOutput>> Func { get; }

        public BufferContinuation(Func<Dataflow<TOutput>> func)
        {
            Func = func;
        }
    }

    public static class Dataflow
    {
        public static Dataflow<T> Return<T>(T value)
        {
            return new Return<T>(value);
        }

        public static Dataflow<T> ReturnMany<T>(IEnumerable<T> value)
        {
            return new ReturnMany<T>(value);
        }

        public static Dataflow<TOutput> Calculation<TInput, TOutput>(DataflowOperator<TInput> @operator,
            Func<TInput, Dataflow<TOutput>> continuation)
        {
            return new DataflowCalculation<TInput, TOutput>(@operator, continuation);
        }

        private static Dataflow<T> Continuation<T>(Func<Dataflow<T>> func)
        {
            return new Continuation<T>(func);
        }

        private static Dataflow<T> ContinuationMany<T>(Func<IEnumerable<Dataflow<T>>> func)
        {
            return new ContinuationMany<T>(func);
        }

        public static Dataflow<TOutput> Bind<TInput, TOutput>(this Dataflow<TInput> dataflow,
            Func<TInput, Dataflow<TOutput>> transform)
        {
            if (dataflow is Return<TInput>)
            {
                var result = ((Return<TInput>)dataflow).Result;
                return Continuation(() => transform(result));
            }
            if (dataflow is ReturnMany<TInput>)
            {
                var result = ((ReturnMany<TInput>)dataflow).Result;
                return ContinuationMany(() => result.Select(transform));
            }
            if (dataflow is Continuation<TInput>)
            {
                var func = ((Continuation<TInput>)dataflow).Func;
                return Continuation(() => func().Bind(transform));
            }
            if (dataflow is ContinuationMany<TInput>)
            {
                var func = ((ContinuationMany<TInput>)dataflow).Func;
                return ContinuationMany(() => func().Select(item => item.Bind(transform)));
            }
            if (dataflow is Buffer<TInput>)
            {
                var bufferDataflow = (Buffer<TInput>)dataflow;
                return new BufferContinuation<TOutput>(() => transform(bufferDataflow.Item));
            }
            throw new InvalidOperationException();
        }

        public static Dataflow<TOutput> Select<TInput, TOutput>(this Dataflow<TInput> source,
            Func<TInput, TOutput> selector)
        {
            return source.Bind(item => Return(selector(item)));
        }

        public static Dataflow<TOutput> SelectMany<TInput, TOutput>(this Dataflow<TInput> source,
            Func<TInput, IEnumerable<TOutput>> selector)
        {
            return source.Bind(input => ReturnMany(selector(input)));
        }

        public static Dataflow<TOutput> SelectMany<TInput, TMedium, TOutput>(this Dataflow<TInput> dataflow,
            Func<TInput, IEnumerable<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return dataflow.SelectMany(input =>
                mediumSelector(input)
                    .Select(medium => resultSelector(input, medium)));
        }

        public static Dataflow<TOutput> SelectMany<TInput, TOutput>(this Dataflow<TInput> source,
            Func<TInput, Dataflow<TOutput>> selector)
        {
            return source.Bind(selector);
        }

        public static Dataflow<TOutput> SelectMany<TInput, TMedium, TOutput>(this Dataflow<TInput> source,
            Func<TInput, Dataflow<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return source.SelectMany(input =>
                mediumSelector(input)
                    .Select(medium => resultSelector(input, medium)));
        }

        public static Dataflow<IList<T>> Buffer<T>(this Dataflow<T> source,
            TimeSpan batchTimeout, int batchMaxSize)
        {
            return source.Bind(item => new Buffer<IList<T>>(List(item).ToList(), batchTimeout, batchMaxSize));
        }
    }
}