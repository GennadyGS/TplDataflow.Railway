using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TplDataflow.Extensions.Example.BusinessObjects;
using TplDataflow.Extensions.Example.Interfaces;
using TplDataFlow.Extensions;

namespace TplDataflow.Extensions.Example.Implementation
{
    internal class EventSetStorageProcessorTplDataflow : IEventSetStorageProcessor
    {
        private readonly EventSetStorageProcessorLogic _logic;
        private readonly IEventSetConfiguration _configuration;

        private readonly BufferBlock<EventDetails> _inputBlock = new BufferBlock<EventDetails>();
        private readonly BufferBlock<EventSetWithEvents> _eventSetCreatedBlock = new BufferBlock<EventSetWithEvents>();
        private readonly BufferBlock<EventSetWithEvents> _eventSetUpdatedBlock = new BufferBlock<EventSetWithEvents>();
        private readonly BufferBlock<EventDetails> _eventSkippedBlock = new BufferBlock<EventDetails>();
        private readonly BufferBlock<EventDetails> _eventFailedBlock = new BufferBlock<EventDetails>();

        public EventSetStorageProcessorTplDataflow(Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService, IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration, Func<DateTime> currentTimeProvider)
        {
            _logic = new EventSetStorageProcessorLogic(repositoryResolver, identityService, processTypeManager, currentTimeProvider);
            _configuration = configuration;

            var dataflowResult = CreateDataflow(_inputBlock);

            dataflowResult.EventSetCreated.LinkWith(_eventSetCreatedBlock);
            dataflowResult.EventSetUpdated.LinkWith(_eventSetUpdatedBlock);
            dataflowResult.EventSkipped.LinkWith(_eventSkippedBlock);
            dataflowResult.EventFailed.LinkWith(_eventFailedBlock);
        }

        public IObserver<EventDetails> Input
        {
            get
            {
                return _inputBlock.AsObserver();
            }
        }

        IObservable<EventSetWithEvents> IEventSetStorageProcessor.EventSetCreatedOutput
        {
            get
            {
                return _eventSetCreatedBlock.AsObservable();
            }
        }

        IObservable<EventSetWithEvents> IEventSetStorageProcessor.EventSetUpdatedOutput
        {
            get
            {
                return _eventSetUpdatedBlock.AsObservable();
            }
        }

        IObservable<EventDetails> IEventSetStorageProcessor.EventSkippedOutput
        {
            get
            {
                return _eventSkippedBlock.AsObservable();
            }
        }

        IObservable<EventDetails> IEventSetStorageProcessor.EventFailedOutput
        {
            get
            {
                return _eventFailedBlock.AsObservable();
            }
        }

        Task IEventSetStorageProcessor.CompletionTask
        {
            get
            {
                return Task.WhenAll(
                    _eventSetCreatedBlock.Completion,
                    _eventSetUpdatedBlock.Completion,
                    _eventSkippedBlock.Completion,
                    _eventFailedBlock.Completion);
            }
        }

        private DataflowResult CreateDataflow(ISourceBlock<EventDetails> input)
        {
            var batchTimeout = TimeSpan.Parse(_configuration.EventBatchTimeout);
            return input
                .Select(_logic.LogEvent)
                .Buffer(batchTimeout, _configuration.EventBatchSize)
                .SelectMany(_logic.SplitEventsIntoGroupsSafe)
                .SelectSafe(_logic.CheckNeedSkipEventGroup)
                .BufferSafe(TimeSpan.Parse(_configuration.EventGroupBatchTimeout), _configuration.EventGroupBatchSize)
                .SelectManySafe(_logic.ProcessEventGroupsBatchSafe)
                .Match(
                    success => success.Map(result => result.IsCreated,
                        resultCreated => resultCreated.Select(result => result.EventSetWithEvents),
                        resultUpdated => resultUpdated.Select(result => result.EventSetWithEvents),
                        (eventSetCreated, eventSetUpdated) => new { eventSetCreated, eventSetUpdated }),
                    failure => failure.Map(result => result.IsSkipped,
                        resultSkipped => resultSkipped.SelectMany(result => result.Events),
                        resultFailed => resultFailed.SelectMany(result => result.Events),
                        (eventSkipped, eventFailed) => new { eventSkipped, eventFailed }),
                    (success, failure) => new DataflowResult
                    {
                        EventSetCreated = success.eventSetCreated,
                        EventSetUpdated = success.eventSetUpdated,
                        EventSkipped = failure.eventSkipped,
                        EventFailed = failure.eventFailed
                    });
        }

        private class DataflowResult
        {
            public ISourceBlock<EventSetWithEvents> EventSetCreated { get; set; }

            public ISourceBlock<EventSetWithEvents> EventSetUpdated { get; set; }

            public ISourceBlock<EventDetails> EventSkipped { get; set; }

            public ISourceBlock<EventDetails> EventFailed { get; set; }
        }
    }
}