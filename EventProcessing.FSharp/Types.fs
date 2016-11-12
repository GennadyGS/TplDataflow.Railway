namespace EventProcessing.FSharp

open System
open LanguageExt
open AsyncProcessing.Core
open Dataflow.Core
open Dataflow.Railway
open EventProcessing.BusinessObjects
open EventProcessing.Interfaces
open EventProcessing.Implementation
open AsyncProcessing.Dataflow
open AsyncProcessing.Dataflow.TplDataflow

[<AbstractClass>]
type internal BaseDataflowIndividualAsyncFactory() = 
    inherit EventSetStorageProcessor.FactoryBase()
    override this.InternalCreateStorageProcessor(logic, configuration) = 
        this.CreateDataflowAsyncProcessor(
            Func<_,_,_>(fun dataflowFactory event ->
                this.ProcessEventDataflow(logic, configuration, dataflowFactory, event)))
    abstract member CreateDataflowAsyncProcessor : 
        Func<IDataflowFactory, EventDetails, IDataflow<EventSetStorageProcessor.Result>> -> 
        IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result>
    member private this.ProcessEventDataflow
        (logic: EventSetStorageProcessor.Logic, configuration: IEventSetConfiguration,
         dataflowFactory: IDataflowFactory, event: EventDetails)
        : IDataflow<EventSetStorageProcessor.Result> = 
        dataflowFactory.Return(event)
            .Select(logic.LogEvent)
            .Buffer(configuration.EventBatchTimeout, configuration.EventBatchSize)
            .SelectManyAsync(fun item -> logic.SplitEventsIntoGroupsSafeAsync(item))
            .SelectSafe(fun item -> logic.FilterSkippedEventGroup(item))
            .GroupBySafe(fun group -> group.EventSetType.GetCode())
            .SelectManySafe(fun innerGroup ->
                innerGroup
                    .ToList()
                    .SelectManyAsync(fun item -> logic.ProcessEventGroupsSafeAsync(item)))
            .SelectMany(fun (res : Either<EventSetStorageProcessor.UnsuccessResult, EventSetStorageProcessor.SuccessResult>) ->
                EventSetStorageProcessor.Logic.TransformResult(res))

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
            TplDataflowDataflowAsyncProcessor<EventDetails, EventSetStorageProcessor.Result>(bindFunc) :> IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result>
