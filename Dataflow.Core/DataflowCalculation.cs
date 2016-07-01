using System;
using System.Collections.Generic;
using System.Linq;

namespace Dataflow.Core
{
    public class DataflowCalculation<TInput, TOutput> : Dataflow<TOutput>
    {
        public DataflowCalculation(DataflowOperator<TInput> @operator, Func<TInput, Dataflow<TOutput>> continuation)
        {
            Operator = @operator;
            Continuation = continuation;
        }

        public override DataflowType<TOutput> GetDataflowType()
        {
            return new DataflowCalculationType<TInput, TOutput>();
        }

        public DataflowOperator<TInput> Operator { get; }

        public Func<TInput, Dataflow<TOutput>> Continuation { get; }

        public override Dataflow<TOutput2> Bind<TOutput2>(Func<TOutput, Dataflow<TOutput2>> bindFunc)
        {
            return Dataflow.Calculation(Operator, item => Continuation(item).Bind(bindFunc));
        }
    }
}