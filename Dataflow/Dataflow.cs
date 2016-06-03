using System;

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

    public class Continuation<TInput, TOutput> : Dataflow<TInput, TOutput>
    {
        public Continuation(Func<TInput, Dataflow<TInput, TOutput>> continuationFunc)
        {
            ContinuationFunc = continuationFunc;
        }

        public Func<TInput, Dataflow<TInput, TOutput>> ContinuationFunc { get; }
    }

    public static class Dataflow
    {
        public static Dataflow<TInput, TOutput> Return<TInput, TOutput>(TOutput value)
        {
            return new Return<TInput, TOutput>(value);
        }

        public static Dataflow<TInput, TOutput> Continuation<TInput, TOutput>(Func<TInput, Dataflow<TInput, TOutput>> continuationFunc)
        {
            return new Continuation<TInput, TOutput>(continuationFunc);
        }

        public static Dataflow<TInput, TOutput2> Bind<TInput, TOutput1, TOutput2>(this Dataflow<TInput, TOutput1> dataflow, 
            Func<TOutput1, Dataflow<TInput, TOutput2>> transform)
        {
            if (dataflow is Return<TInput, TOutput1>)
            {
                var resultDataflow = (Return<TInput, TOutput1>)dataflow;
                return transform(resultDataflow.Result);
            }
            if (dataflow is Continuation<TInput, TOutput1>)
            {
                var continuationDataflow = (Continuation<TInput, TOutput1>)dataflow;
                return Continuation((TInput input) =>
                    Bind(continuationDataflow.ContinuationFunc(input), transform));
            }
            throw new InvalidOperationException();
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
            //if (dataflow is Continuation<TInput, TOutput1>)
            //{
            //    var continuationDataflow = (Continuation<TInput, TOutput1>)dataflow;
            //    return Continuation((TInput input) =>
            //        Bind(continuationDataflow.ContinuationFunc(input), mediumSelector))
            //            .Bind(medium =>
            //                Return<TInput, TOutput2>(resultSelector(input, medium))));
            //}
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