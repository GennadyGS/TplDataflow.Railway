using System;

namespace Dataflow.Core
{
    public class DataflowCalculation<TInput, TOutput> : Dataflow<TOutput>
    {
        public DataflowOperator<TInput> Operator { get; }

        public Func<TInput, Dataflow<TOutput>> Continuation { get; }

        public DataflowCalculation(IDataflowFactory factory, IDataflowType<TOutput> type, 
            DataflowOperator<TInput> @operator, Func<TInput, Dataflow<TOutput>> continuation) 
            : base(factory, type)
        {
            Operator = @operator;
            Continuation = continuation;
        }

        public override Dataflow<TOutput2> Bind<TOutput2>(Func<TOutput, Dataflow<TOutput2>> bindFunc)
        {
            return Factory.Calculation(Operator, item => Continuation(item).Bind(bindFunc));
        }
    }
}