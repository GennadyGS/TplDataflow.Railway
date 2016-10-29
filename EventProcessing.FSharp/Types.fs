namespace EventProcessing.FSharp

open System
open EventProcessing.Implementation

[<AbstractClass>]
type BaseDataflowIndividualAsyncFactory() = 
    inherit EventSetStorageProcessor.FactoryBase()
    override this.InternalCreateStorageProcessor(logic, configuration) = 
        raise (NotImplementedException())

type ObservableDataflowIndividualAsyncFactory() = 
    inherit BaseDataflowIndividualAsyncFactory()

type TplDataflowDataflowIndividualAsyncFactory() = 
    inherit BaseDataflowIndividualAsyncFactory()
