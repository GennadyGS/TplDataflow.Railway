namespace Dataflow.FSharp.Tests

open System
open System.Linq
open System.Threading.Tasks
open Dataflow.Core
open Dataflow.FSharp
open Xunit

[<AbstractClass>]
type DataflowTests(transformer : IDataflowSequenceTransformer) = 
    let testBindDataflow (input : 'input seq) (expectedOutput : 'output seq) 
        (dataflowBindFunc : IDataflowFactory -> 'input -> 'output IDataflow) = 
        let output = input |> transformer.TransformSequence dataflowBindFunc
        let expectedOutputList = expectedOutput |> Seq.toList
        let outputList = output |> Seq.toList
        Assert.Equal<'output list>(expectedOutputList, outputList)

    [<Fact>]
    let ``Bind Return Dataflow Should Return The Same List``() = 
        let input = [ 1..3 ]
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { return i })
    
    [<Fact>]
    let ``Bind Return! Dataflow Should Return The Same List``() = 
        let input = [ 1..3 ]
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { return! dataflowFactory.Return i })
    
    [<Fact>]
    let ``Bind Return! Async Dataflow Should Return The Same List``() = 
        let input = [ 1..3 ]
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { return! dataflowFactory.ReturnAsync (Task.FromResult i) })
    
    [<Fact>]
    let ``Bind Return Dataflow Should Return The Same List Of Strings``() = 
        let input = [ "1"; "2"; "3" ]
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { return i })

    [<Fact>]
    let ``Bind Return Dataflow Should Return The Projected List``() =
        let input = [ 1.. 3]
        let expectedOutput = input |> List.map (fun i -> i * 2) 
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { return 2 * i })

    [<Fact>]
    let ``Bind Let! and Return Dataflow Should Return The Projected List``() =
        let input = [ 1.. 3]
        let expectedOutput = input |> List.map (fun i -> i * 2) 
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { 
                let! i' = dataflowFactory.Return i
                return 2 * i' 
            })

        let input = [ 1.. 3]
        let expectedOutput = 
            input 
                |> List.map (fun i -> i * 2)  
                |> List.map (fun i -> i + 1) 
                |> List.map (fun i -> i * 2) 
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { 
                let! a = dataflowFactory.Return (i * 2)
                let! b = dataflowFactory.Return (a + 1)
                let! c = dataflowFactory.Return (b * 2)
                return c
            })

    [<Fact>]
    let ``Bind ReturnMany Dataflow Should Return The Projected List``() =
        let input = [ 1.. 3]
        let expectedOutput = seq { 
            for i in input do
            for j in 1..2 do
            yield i * 2 + 1
        }
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { 
                return! dataflowFactory.ReturnMany (Enumerable.Repeat (i * 2 + 1, 2))
            })

    [<Fact>]
    let ``Bind Let! and ReturnMany Dataflow Should Return The Projected List``() =
        let input = [ 1.. 3]
        let expectedOutput = seq { 
            for i in input do
            for j in 1..2 do
            yield i * 2 + 1
        }
        testBindDataflow input expectedOutput (fun dataflowFactory i -> 
            DataflowBuilder(dataflowFactory) { 
                let! a = dataflowFactory.ReturnMany (Enumerable.Repeat (i * 2, 2)) 
                let! b = dataflowFactory.Return (a + 1) 
                return b 
            })

type EnumerableDataflowTests() = 
    inherit DataflowTests(EnumerableDataflowSequenceTransformer())

type ObservableDataflowTests() = 
    inherit DataflowTests(ObservableDataflowSequenceTransformer())

type TplDataflowDataflowTests() = 
    inherit DataflowTests(TplDataflowDataflowSequenceTransformer())
