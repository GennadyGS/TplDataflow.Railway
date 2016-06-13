using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public abstract class Dataflow<T>
    {
    }

    public class Return<T> : Dataflow<T>
    {
        public Return(T result)
        {
            Result = result;
        }

        public T Result { get; }
    }

    public class ReturnMany<T> : Dataflow<T>
    {
        public ReturnMany(IEnumerable<T> result)
        {
            Result = result;
        }

        public IEnumerable<T> Result { get; }
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

    public static class Dataflow
    {
        public static Dataflow<TOutput> Return<TOutput>(TOutput value)
        {
            return new Return<TOutput>(value);
        }

        public static Dataflow<TOutput> ReturnMany<TOutput>(IEnumerable<TOutput> value)
        {
            return new ReturnMany<TOutput>(value);
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

        public static Dataflow<TOutput> SelectMany<TInput, TMedium, TOutput>(this Dataflow<TInput> dataflow,
            Func<TInput, Dataflow<TMedium>> mediumSelector,
            Func<TInput, TMedium, TOutput> resultSelector)
        {
            return dataflow.SelectMany(input => 
                mediumSelector(input)
                    .Select(medium => resultSelector(input, medium)));
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