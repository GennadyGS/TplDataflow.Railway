namespace Dataflow.FSharp.Tests

open System.Reactive.Linq
open Dataflow.Rx

type ObservableDataflowSequenceTransformer() = 
    interface IDataflowSequenceTransformer with
        member this.TransformSequence dataflowBindFunc input =
            input.ToObservable()
                .BindDataflow(System.Func<_,_,_>(dataflowBindFunc))
                .ToEnumerable();
