using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using TplDataflow.Extensions.Example.BusinessObjects;
using TplDataflow.Extensions.Example.Interfaces;
using TplDataFlow.Extensions;

namespace TplDataflow.Extensions.Example.Implementation
{
    public class EventSetStorageProcessorEnumerable : IEventSetStorageProcessor
    {
        private readonly EventSetStorageProcessorLogic _logic;
        private readonly IEventSetConfiguration _configuration;

        private readonly Subject<EventDetails> _input = new Subject<EventDetails>();
        private readonly Subject<EventSetWithEvents> _eventSetCreatedOutput = new Subject<EventSetWithEvents>();
        private readonly Subject<EventSetWithEvents> _eventSetUpdatedOutput = new Subject<EventSetWithEvents>();
        private readonly Subject<EventDetails> _eventFailedOutput = new Subject<EventDetails>();
        private readonly Subject<EventDetails> _eventSkippedOutput = new Subject<EventDetails>();
        private readonly Task _completionTask;

        public EventSetStorageProcessorEnumerable(Func<IEventSetRepository> repositoryResolver,
            IIdentityManagementService identityService, IEventSetProcessTypeManager processTypeManager,
            IEventSetConfiguration configuration, Func<DateTime> currentTimeProvider)
        {
            _logic = new EventSetStorageProcessorLogic(repositoryResolver, 
                identityService, processTypeManager, currentTimeProvider);
            _configuration = configuration;

            _completionTask = Task.Run(async () =>
            {
                var dataflowResult = Process(_input.ToEnumerable().ToList());
                dataflowResult.EventSetCreated.Subscribe(_eventSetCreatedOutput);
                dataflowResult.EventSetUpdated.Subscribe(_eventSetUpdatedOutput);
                dataflowResult.EventSkipped.Subscribe(_eventSkippedOutput);
                dataflowResult.EventFailed.Subscribe(_eventFailedOutput);
                await _eventSetCreatedOutput.LastOrDefaultAsync();
                await _eventSetUpdatedOutput.LastOrDefaultAsync();
                await _eventSkippedOutput.LastOrDefaultAsync();
                await _eventFailedOutput.LastOrDefaultAsync();
            });
        }

        IObserver<EventDetails> IEventSetStorageProcessor.Input
        {
            get
            {
                return _input;
            }
        }

        IObservable<EventSetWithEvents> IEventSetStorageProcessor.EventSetCreatedOutput
        {
            get
            {
                return _eventSetCreatedOutput;
            }
        }

        IObservable<EventSetWithEvents> IEventSetStorageProcessor.EventSetUpdatedOutput
        {
            get
            {
                return _eventSetUpdatedOutput;
            }
        }

        IObservable<EventDetails> IEventSetStorageProcessor.EventSkippedOutput
        {
            get
            {
                return _eventSkippedOutput;
            }
        }

        IObservable<EventDetails> IEventSetStorageProcessor.EventFailedOutput
        {
            get
            {
                return _eventFailedOutput;
            }
        }

        Task IEventSetStorageProcessor.CompletionTask
        {
            get
            {
                return _completionTask;
            }
        }

        private async Task GetCompletionTask()
        {
            await _eventSetCreatedOutput.LastOrDefaultAsync();
            await _eventSetUpdatedOutput.LastOrDefaultAsync();
            await _eventSkippedOutput.LastOrDefaultAsync();
            await _eventFailedOutput.LastOrDefaultAsync();
        }

        private DataflowResult Process(IEnumerable<EventDetails> input)
        {
            return input
                .Select(_logic.LogEvent)
                .Buffer(TimeSpan.Parse(_configuration.EventBatchTimeout), _configuration.EventBatchSize)
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
            public IEnumerable<EventSetWithEvents> EventSetCreated { get; set; }

            public IEnumerable<EventSetWithEvents> EventSetUpdated { get; set; }

            public IEnumerable<EventDetails> EventSkipped { get; set; }

            public IEnumerable<EventDetails> EventFailed { get; set; }
        }
    }
}