using System;
using System.Collections.Generic;
using System.Linq;
using Railway.Linq;
using static LanguageExt.Prelude;

namespace Dataflow.Core
{
    public abstract class Dataflow<T>
    {
        public abstract Dataflow<TOutput> Bind<TOutput>(Func<T, Dataflow<TOutput>> bindFunc);

        public abstract IEnumerable<T> TansformEnumerableOfDataFlow(IEnumerable<Dataflow<T>> dataflows);
    }

    public abstract class DataflowOperator<T> : Dataflow<T>
    {
        public override Dataflow<TOutput> Bind<TOutput>(Func<T, Dataflow<TOutput>> bindFunc)
        {
            return Dataflow.Calculation(this, bindFunc);
        }
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

        public override Dataflow<TOutput2> Bind<TOutput2>(Func<TOutput, Dataflow<TOutput2>> bindFunc)
        {
            return Dataflow.Calculation(Operator, item => Continuation(item).Bind(bindFunc));
        }

        public override IEnumerable<TOutput> TansformEnumerableOfDataFlow(IEnumerable<Dataflow<TOutput>> dataflows)
        {
            var calculationDataflows = dataflows.Cast<DataflowCalculation<TInput, TOutput>>();
            var inputDataflows = Operator.TansformEnumerableOfDataFlow(calculationDataflows.Select(dataflow => dataflow.Operator));
            var outputDataflows = inputDataflows.Select(Continuation);
            if (!outputDataflows.Any())
            {
                return Enumerable.Empty<TOutput>();
            }
            return outputDataflows.First().TansformEnumerableOfDataFlow(outputDataflows);
        }
    }

    public class Return<T> : DataflowOperator<T>
    {
        public Return(T result)
        {
            Result = result;
        }

        public T Result { get; }

        public override IEnumerable<T> TansformEnumerableOfDataFlow(IEnumerable<Dataflow<T>> dataflows)
        {
            return dataflows.Cast<Return<T>>().Select(dataflow => dataflow.Result);
        }
    }

    public class ReturnMany<T> : DataflowOperator<T>
    {
        public ReturnMany(IEnumerable<T> result)
        {
            Result = result;
        }

        public IEnumerable<T> Result { get; }

        public override IEnumerable<T> TansformEnumerableOfDataFlow(IEnumerable<Dataflow<T>> dataflows)
        {
            return dataflows.Cast<ReturnMany<T>>().SelectMany(dataflow => dataflow.Result);
        }
    }

    public class Buffer<T> : DataflowOperator<IList<T>>
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

        public override IEnumerable<IList<T>> TansformEnumerableOfDataFlow(IEnumerable<Dataflow<IList<T>>> dataflows)
        {
            return dataflows
                .Cast<Buffer<T>>()
                .Select(item => item.Item)
                .Buffer(BatchMaxSize);
        }
    }

    public static class Dataflow
    {
        public static Dataflow<TOutput> Calculation<TInput, TOutput>(DataflowOperator<TInput> @operator,
            Func<TInput, Dataflow<TOutput>> continuation)
        {
            return new DataflowCalculation<TInput, TOutput>(@operator, continuation);
        }

        public static Dataflow<T> Return<T>(T value)
        {
            return new Return<T>(value);
        }

        public static Dataflow<T> ReturnMany<T>(IEnumerable<T> value)
        {
            return new ReturnMany<T>(value);
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
            return source.Bind(item => new Buffer<T>(item, batchTimeout, batchMaxSize));
        }
    }
}