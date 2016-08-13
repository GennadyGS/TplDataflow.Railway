namespace Dataflow.FSharp.Tests


module DataflowCoreExtensions =
    open Dataflow.Core

    let enumerableDataflowBinder 
        (dataflowBindFunc: IDataflowFactory -> 'input -> 'output IDataflow) 
        (input: 'input seq) 
        : seq<'output> =
            input.BindDataflow(System.Func<_,_,_>(dataflowBindFunc))
