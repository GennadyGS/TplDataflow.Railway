namespace Dataflow.FSharp.Tests

open Xunit
open Swensen.Unquote
open Dataflow.Core
open DataflowCoreExtensions
open RxDataflowExtensions
open TplDataflowDataflowExtensions

[<AbstractClass>]
type DataflowTests (dataflowBinderFunc: (IDataflowFactory -> 'input -> 'output IDataflow) -> 'input seq -> 'output seq) = 
    let testBindDataflow 
        (input : 'input seq)
        (expectedOutput : 'output seq)
        (dataflowBindFunc : IDataflowFactory -> 'input -> 'output IDataflow)=
            let output = input |> dataflowBinderFunc dataflowBindFunc 
            test <@ (expectedOutput |> Seq.toList) = (output |> Seq.toList) @>           

    [<Fact>]
    let ``Bind Return Dataflow Should Return The Same List``() = 
        let input = [ 1..3 ]
        let expectedOutput = input
        testBindDataflow input expectedOutput (fun dataflowFactory i -> dataflowFactory.Return i)

type EnumerableDataflowTests() =
    inherit DataflowTests(enumerableDataflowBinder)

type ObservableDataflowTests() =
    inherit DataflowTests(observableDataflowBinder)

type TplDataflowDataflowTests() =
    inherit DataflowTests(tplDataflowDataflowBinder)
