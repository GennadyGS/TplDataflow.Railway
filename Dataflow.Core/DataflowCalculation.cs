using System;

namespace Dataflow.Core
{
    public class DataflowCalculation<TInput, TOutput> : Dataflow<TOutput>
    {
        public DataflowOperator<TInput> Operator { get; }

        public Func<TInput, IDataflow<TOutput>> Continuation { get; }

        public DataflowCalculation(IDataflowFactory factory, IDataflowType<TOutput> type, 
            DataflowOperator<TInput> @operator, Func<TInput, IDataflow<TOutput>> continuation) 
            : base(factory, type)
        {
            Operator = @operator;
            Continuation = continuation;
        }

        public override IDataflow<TOutput2> Bind<TOutput2>(Func<TOutput, IDataflow<TOutput2>> bindFunc)
        {
            return Factory.Calculation(Operator, item => Continuation(item).Bind(bindFunc));
        }
    }
}