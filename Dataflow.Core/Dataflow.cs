using System;
using System.ComponentModel;

namespace Dataflow.Core
{
    public class Dataflow<TInput, TOutput>
    {
    }

    public class Return<TInput, TOutput> : Dataflow<TInput, TOutput>
    {
        public Return(TOutput result)
        {
            Result = result;
        }

        public TOutput Result { get; }
    }

    public class Continuation<TInput, TMedium, TOutput> : Dataflow<TInput, TOutput>
    {
        public Dataflow<TInput, TMedium> Dataflow { get; }

        public Func<TMedium, Dataflow<TMedium, TOutput>> ContinuationFunc { get; }

        public Continuation(Dataflow<TInput, TMedium> dataflow, Func<TMedium, Dataflow<TMedium, TOutput>> continuationFunc)
        {
            Dataflow = dataflow;
            ContinuationFunc = continuationFunc;
        }
    }

    public static class Dataflow
    {
        public static Dataflow<TInput, TOutput> Return<TInput, TOutput>(TOutput value)
        {
            return new Return<TInput, TOutput>(value);
        }

        public static Dataflow<TInput, TOutput> Bind<TInput, TMedium, TOutput>(this Dataflow<TInput, TMedium> dataflow, 
            Func<TMedium, Dataflow<TInput, TOutput>> transform)
        {
            if (dataflow is Return<TInput, TMedium>)
            {
                var resultDataflow = (Return<TInput, TMedium>)dataflow;
                return transform(resultDataflow.Result);
            }
            throw new NotImplementedException();
        }

        public static Dataflow<TInput, TOutput> Select<TInput, TMedium, TOutput>(this Dataflow<TInput, TMedium> source,
            Func<TMedium, TOutput> selector)
        {
            return Bind(source, item => Return<TInput, TOutput>(selector(item)));
        }

        public static Dataflow<TInput, TOutput2> SelectMany<TInput, TOutput1, TMedium, TOutput2>(this Dataflow<TInput, TOutput1> dataflow,
            Func<TOutput1, Dataflow<TInput, TMedium>> mediumSelector, 
            Func<TOutput1, TMedium, TOutput2> resultSelector)
        {
            if (dataflow is Return<TInput, TOutput1>)
            {
                var resultDataflow = (Return<TInput, TOutput1>)dataflow;
                return mediumSelector(resultDataflow.Result)
                    .Bind(medium => 
                        Return<TInput, TOutput2>(resultSelector(resultDataflow.Result, medium)));
            }
            throw new InvalidOperationException();
        }
    }

    public class DataflowTests
    {
        public void Test1()
        {
            var dataflow1 = Dataflow.Return<int, int>(1)
                .Bind(x =>
                    {
                        return Dataflow.Return<int, int>(2)
                            .Bind(y =>
                                {
                                    var r = x + y;
                                    return Dataflow.Return<int, int>(r);
                                });
                    });

            var dataFlow2 = from x in Dataflow.Return<int, int>(1)
                            from y in Dataflow.Return<int, int>(2)
                            select x + y;
        }
    }
}