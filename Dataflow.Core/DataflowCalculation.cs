using System;

namespace Dataflow.Core
{
    public class DataflowCalculation<TInput, TOutput> : Dataflow<TOutput>
    {

        private static readonly DataflowType<TOutput> DataflowType = new DataflowCalculationType<TInput,TOutput>();

        public DataflowOperator<TInput> Operator { get; }

        public Func<TInput, Dataflow<TOutput>> Continuation { get; }

        public DataflowCalculation(DataflowOperator<TInput> @operator, Func<TInput, Dataflow<TOutput>> continuation)
        {
            Operator = @operator;
            Continuation = continuation;
        }

        public override DataflowType<TOutput> GetDataflowType()
        {
            return DataflowType;
        }

        public override Dataflow<TOutput2> Bind<TOutput2>(Func<TOutput, Dataflow<TOutput2>> bindFunc)
        {
            return Dataflow.Calculation(Operator, item => Continuation(item).Bind(bindFunc));
        }
    }
}