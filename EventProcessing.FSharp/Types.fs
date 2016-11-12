namespace EventProcessing.FSharp

open System
open LanguageExt
open AsyncProcessing.Core
open Railway.Linq
open Dataflow.Core
open Dataflow.Railway
open EventProcessing.BusinessObjects
open EventProcessing.Interfaces
open EventProcessing.Implementation
open AsyncProcessing.Dataflow
open AsyncProcessing.Dataflow.TplDataflow
open Dataflow.FSharp

[<AbstractClass>]
type internal BaseDataflowIndividualAsyncFactory() = 
    inherit EventSetStorageProcessor.FactoryBase()
    
    override this.InternalCreateStorageProcessor(logic, configuration) = 
        this.CreateDataflowAsyncProcessor(
            Func<_,_,_>(fun dataflowFactory event ->
                this.ProcessEventDataflow logic configuration dataflowFactory event))
    
    abstract member CreateDataflowAsyncProcessor : 
        Func<IDataflowFactory, EventDetails, IDataflow<EventSetStorageProcessor.Result>> -> 
        IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result>
    
    member private this.ProcessEventDataflow
        (logic : EventSetStorageProcessor.Logic) (configuration : IEventSetConfiguration)
        (dataflowFactory : IDataflowFactory) (event : EventDetails) = 
            DataflowBuilder(dataflowFactory) {
                let! event' = dataflowFactory.Return (logic.LogEvent event)
                let! eventBuffer = 
                    dataflowFactory.Buffer (event', configuration.EventBatchTimeout, configuration.EventBatchSize)
                let! eventGroup = 
                    dataflowFactory.ReturnManyAsync (logic.SplitEventsIntoGroupsSafeAsync(eventBuffer))
                // TODO: Simplify
                let! eventGroup' = 
                    dataflowFactory.Return (eventGroup.SelectSafe(fun item -> 
                        logic.FilterSkippedEventGroup(item)))
                // TODO: Simplify
                let! eventGroupGroup = 
                    dataflowFactory
                        .Return(eventGroup')
                        .GroupBySafe(fun group -> group.EventSetType.GetCode())
                // TODO: Simplify
                let! result = 
                    dataflowFactory.Return(eventGroupGroup)
                        .SelectManySafe(fun innerGroup ->
                            innerGroup
                                .ToList()
                                .SelectManyAsync(fun item -> logic.ProcessEventGroupsSafeAsync(item)))
                return! dataflowFactory.ReturnMany(EventSetStorageProcessor.Logic.TransformResult(result))
            }

type internal ObservableDataflowIndividualAsyncFactory() = 
    inherit BaseDataflowIndividualAsyncFactory()
    
    override this.CreateDataflowAsyncProcessor
        (bindFunc : Func<IDataflowFactory, EventDetails, IDataflow<EventSetStorageProcessor.Result>>)
        : IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result> =
            DataflowAsyncProcessor<EventDetails, EventSetStorageProcessor.Result>(bindFunc) 
                :> IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result>

type internal TplDataflowDataflowIndividualAsyncFactory() = 
    inherit BaseDataflowIndividualAsyncFactory()
    
    override this.CreateDataflowAsyncProcessor
        (bindFunc : Func<IDataflowFactory, EventDetails, IDataflow<EventSetStorageProcessor.Result>>) 
        : IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result> =
            TplDataflowDataflowAsyncProcessor<EventDetails, EventSetStorageProcessor.Result>(bindFunc) 
                :> IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result>
