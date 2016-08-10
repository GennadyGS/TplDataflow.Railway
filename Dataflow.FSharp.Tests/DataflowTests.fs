namespace Dataflow.FSharp.Tests

open Xunit
open Swensen.Unquote
open Dataflow.Core

type DataflowTests() = 
    
    let TestBindDataflow (input: seq<'input>) (expectedOutput: seq<'output>) 
        (dataflowBindFunc: IDataflowFactory -> 'input -> IDataflow<'output>) =
            let inputList = input |> Seq.toList
            let expectedOutputList = expectedOutput |> Seq.toList
            let output = input.BindDataflow(System.Func<_,_,_>(dataflowBindFunc)) |> Seq.toList
            test <@ expectedOutputList = output @>           

    [<Fact>]
    let ``Bind Return Dataflow Should Return The Same List`` ()  = 
        let input =  [|1, 2, 3 |];
        let expectedOutput = input |> Array.copy;
        TestBindDataflow input expectedOutput (fun dataflowFactory i -> dataflowFactory.Return i)
          