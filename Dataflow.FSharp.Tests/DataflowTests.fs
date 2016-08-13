namespace Dataflow.FSharp.Tests

open Xunit
open TestBindDataflow

type DataflowTests() = 
    [<Fact>]
    let ``Bind Return Dataflow Should Return The Same List``() = 
        let input = [ 1..3 ]
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> dataflowFactory.Return i)
