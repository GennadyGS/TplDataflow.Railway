namespace Dataflow.FSharp.Tests

open Dataflow.Core

type EnumerableDataflowSequenceTransformer() = 
    interface IDataflowSequenceTransformer with
        member this.TransformSequence dataflowBindFunc input =
            input.BindDataflow(System.Func<_,_,_>(dataflowBindFunc))
