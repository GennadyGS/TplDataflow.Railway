namespace Dataflow.FSharp.Tests

module TplDataflowDataflowExtensions = 
    open Dataflow.Core
    open System.Reactive.Linq
    open System.Threading.Tasks.Dataflow
    open Dataflow.TplDataflow
    open System

    let tplDataflowDataflowBinder 
        (dataflowBindFunc: IDataflowFactory -> 'input -> 'output IDataflow) 
        (input: 'input seq) 
        : seq<'output> =
            let block = BufferBlock<'input>()
            input.ToObservable().Subscribe(block.AsObserver()) |> ignore
            block
                .BindDataflow(Func<_,_,_>(dataflowBindFunc))
                .AsObservable()
                .ToEnumerable();

