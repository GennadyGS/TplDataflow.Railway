namespace Dataflow.FSharp.Tests

open Xunit
open Swensen.Unquote

type DataflowTests() = 
    
    [<Fact>]
    let ``Test`` ()  = 
        test <@ 2 + 2 = 4 @>           
