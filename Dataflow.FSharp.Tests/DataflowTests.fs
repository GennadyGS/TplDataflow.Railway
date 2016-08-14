namespace Dataflow.FSharp.Tests

open Xunit
open Swensen.Unquote
open Dataflow.Core

[<AbstractClass>]
type DataflowTests(transformer : IDataflowSequenceTransformer) = 
    
    let testBindDataflow (input : 'input seq) (expectedOutput : 'output seq) 
        (dataflowBindFunc : IDataflowFactory -> 'input -> 'output IDataflow) = 
        let output = input |> transformer.TransformSequence dataflowBindFunc
        test <@ (expectedOutput |> Seq.toList) = (output |> Seq.toList) @>
    
    [<Fact>]
    let ``Bind Return Dataflow Should Return The Same List``() = 
        let input = [ 1..3 ]
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> dataflowFactory.Return i)
    
    [<Fact>]
    let ``Bind Return Dataflow Should Return The Same List Of Strings``() = 
        let input = [ "1"; "2"; "3" ]
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> dataflowFactory.Return i)

type EnumerableDataflowTests() = 
    inherit DataflowTests(EnumerableDataflowSequenceTransformer())

type ObservableDataflowTests() = 
    inherit DataflowTests(ObservableDataflowSequenceTransformer())

type TplDataflowDataflowTests() = 
    inherit DataflowTests(TplDataflowDataflowSequenceTransformer())
