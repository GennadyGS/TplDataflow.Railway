namespace Dataflow.FSharp.Tests

open Dataflow.Core

type IDataflowSequenceTransformer =
    abstract member TransformSequence : (IDataflowFactory -> 'input -> 'output IDataflow) -> 'input seq -> 'output seq
