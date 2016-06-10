using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Dataflow.Core
{
    public abstract class Dataflow<T>
    {
        public abstract bool IsReturn { get; }
    }

    public class Return<T> : Dataflow<T>
    {
        public Return(T result)
        {
            Result = result;
        }

        public T Result { get; }

        public override bool IsReturn => true;
    }

    public class Continuation<TOutput> : Dataflow<TOutput>
    {
        public Func<Dataflow<TOutput>> Func { get; }

        public Continuation(Func<Dataflow<TOutput>> func)
        {
            Func = func;
        }

        public override bool IsReturn => false;
    }

    public class SelectContinuation<TInput, TOutput> : Dataflow<TOutput>
    {
        public TInput Input { get; set; }

        public Func<TInput, TOutput> Selector { get; set; }

        public SelectContinuation(TInput input, Func<TInput, TOutput> selector)
        {
            Input = input;
            Selector = selector;
        }

        public override bool IsReturn => false;
    }

    public class SelectManyContinuation<TInput, TOutput>: Dataflow<TOutput>
    {
        public TInput Input { get; set; }

        public Func<TInput, IEnumerable<TOutput>> Selector { get; set; }

        public SelectManyContinuation(TInput input, Func<TInput, IEnumerable<TOutput>> selector)
        {
            Input = input;
            Selector = selector;
        }

        public override bool IsReturn => false;
    }

    public static class Dataflow
    {
        public static Dataflow<TOutput> Return<TOutput>(TOutput value)
        {
            return new Return<TOutput>(value);
        }

        public static Dataflow<TOutput> Bind<TInput, TOutput>(this Dataflow<TInput> dataflow, 
            Func<TInput, Dataflow<TOutput>> transform)
        {
            if (dataflow.IsReturn)
            {
                var result = ((Return<TInput>)dataflow).Result;
                return Continuation(() => transform(result));
            }
            var func = ((Continuation<TInput>) dataflow).Func;
            return Continuation(() => func().Bind(transform));
        }

        public static Dataflow<TOutput> Select<TInput, TOutput>(this Dataflow<TInput> source,
            Func<TInput, TOutput> selector)
        {
            return Bind(source, item => new SelectContinuation<TInput, TOutput>(item, selector));
        }

        public static Dataflow<TOutput> SelectMany<TInput, TOutput>(this Dataflow<TInput> source,
            Func<TInput, IEnumerable<TOutput>> selector)
        {
            return Bind(source, input => new SelectManyContinuation<TInput, TOutput>(input, selector));
        }

        public static Dataflow<TOutput> SelectMany<TInput, TMedium, TOutput>(this Dataflow<TInput> dataflow,
            Func<TInput, Dataflow<TMedium>> mediumSelector, 
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            if (dataflow is Return<TInput>)
            {
                var resultDataflow = (Return<TInput>)dataflow;
                return mediumSelector(resultDataflow.Result)
                    .Bind(medium => 
                        Return(resultSelector(resultDataflow.Result, medium)));
            }
            throw new InvalidOperationException();
        }

        private static Dataflow<T> Continuation<T>(Func<Dataflow<T>> func)
        {
            return new Continuation<T>(func);
        }
    }

    public class DataflowTests
    {
        public void Test1()
        {
            var dataflow1 = Dataflow.Return(1)
                .Bind(x =>
                    {
                        return Dataflow.Return(2)
                            .Bind(y =>
                                {
                                    var r = x + y;
                                    return Dataflow.Return(r);
                                });
                    });

            var dataFlow2 = from x in Dataflow.Return(1)
                            from y in Dataflow.Return(2)
                            select x + y;
        }
    }
}