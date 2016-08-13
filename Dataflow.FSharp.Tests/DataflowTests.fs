namespace Dataflow.FSharp.Tests

open Xunit
open Swensen.Unquote
open Dataflow.Core

type DataflowTests() = 
    
    let testBindDataflow (input: seq<'input>) (expectedOutput: seq<'output>) 
        (dataflowBindFunc: IDataflowFactory -> 'input -> IDataflow<'output>) =
            let output = input.BindDataflow(System.Func<_,_,_>(dataflowBindFunc))
            test <@ (expectedOutput |> Seq.toList) = (output |> Seq.toList) @>           

    [<Fact>]
    let ``Bind Return Dataflow Should Return The Same List`` ()  = 
        let input = [1 .. 3];
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> dataflowFactory.Return i)
          