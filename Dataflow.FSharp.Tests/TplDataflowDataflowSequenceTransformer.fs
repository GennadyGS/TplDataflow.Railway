namespace Dataflow.FSharp.Tests

open System
open System.Reactive.Linq
open System.Threading.Tasks.Dataflow
open Dataflow.TplDataflow

type TplDataflowDataflowSequenceTransformer() = 
    interface IDataflowSequenceTransformer with
        member this.TransformSequence dataflowBindFunc input =
            let block = BufferBlock<'input>()
            input.ToObservable().Subscribe(block.AsObserver()) |> ignore
            block
                .BindDataflow(Func<_,_,_>(dataflowBindFunc))
                .AsObservable()
                .ToEnumerable();
