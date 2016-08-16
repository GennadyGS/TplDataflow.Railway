namespace Dataflow.FSharp

open Dataflow.Core
open System

type DataflowBuilder(factory : IDataflowFactory) =
    member x.Bind(comp : 'input IDataflow, func : 'input -> 'output IDataflow) =  comp.Bind(Func<_,_>(func))
    member x.Return(value) = factory.Return value

