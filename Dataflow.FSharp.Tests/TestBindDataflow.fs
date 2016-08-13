namespace Dataflow.FSharp.Tests


module TestBindDataflow =
    open Swensen.Unquote
    open EnumerableDataflowBinder

    let testBindDataflowWithBinder input expectedOutput dataflowBindFunc binderFunc =
        let output = input |> binderFunc dataflowBindFunc 
        test <@ (expectedOutput |> Seq.toList) = (output |> Seq.toList) @>           

    let testBindDataflow input expectedOutput dataflowBindFunc =
        testBindDataflowWithBinder input expectedOutput dataflowBindFunc enumerableDataflowBinder
