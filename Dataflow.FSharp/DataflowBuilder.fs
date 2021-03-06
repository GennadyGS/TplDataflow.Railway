﻿namespace Dataflow.FSharp

open Dataflow.Core
open System

type DataflowBuilder(factory : IDataflowFactory) =
    member this.Return(value) = factory.Return value
    member this.ReturnFrom(value) = value
    member this.Bind(comp : 'input IDataflow, func : 'input -> 'output IDataflow) = comp.Bind(Func<_,_>(func))
