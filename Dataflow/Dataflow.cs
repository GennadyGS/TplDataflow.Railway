using System;

namespace Dataflow
{
    public abstract class Dataflow<TInput, TOutput>
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
}