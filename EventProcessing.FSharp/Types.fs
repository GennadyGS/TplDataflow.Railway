namespace EventProcessing.FSharp

open System
open EventProcessing.Implementation

[<AbstractClass>]
type internal BaseDataflowIndividualAsyncFactory() = 
    inherit EventSetStorageProcessor.FactoryBase()
    override this.InternalCreateStorageProcessor(logic, configuration) = 
        raise (NotImplementedException())

type internal ObservableDataflowIndividualAsyncFactory() = 
    inherit BaseDataflowIndividualAsyncFactory()

type internal TplDataflowDataflowIndividualAsyncFactory() = 
    inherit BaseDataflowIndividualAsyncFactory()
