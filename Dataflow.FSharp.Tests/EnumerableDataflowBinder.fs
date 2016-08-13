namespace Dataflow.FSharp.Tests


module EnumerableDataflowBinder =
    open Dataflow.Core

    let enumerableDataflowBinder 
        (dataflowBindFunc: IDataflowFactory -> 'input -> IDataflow<'output>) 
        (input: seq<'input>) 
        : seq<'output> =
            input.BindDataflow(System.Func<_,_,_>(dataflowBindFunc))
