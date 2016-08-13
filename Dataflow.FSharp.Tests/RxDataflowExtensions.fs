namespace Dataflow.FSharp.Tests

module RxDataflowExtensions = 
    open Dataflow.Core
    open Dataflow.Rx
    open System.Reactive.Linq

    let observableDataflowBinder 
        (dataflowBindFunc: IDataflowFactory -> 'input -> 'output IDataflow) 
        (input: 'input seq) 
        : seq<'output> =
            input.ToObservable()
                .BindDataflow(System.Func<_,_,_>(dataflowBindFunc))
                .ToEnumerable();

