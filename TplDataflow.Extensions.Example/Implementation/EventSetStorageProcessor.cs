using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
using TplDataflow.Extensions.Example.BusinessObjects;
using TplDataflow.Extensions.Example.Exceptions;
using TplDataflow.Extensions.Example.Interfaces;
using TplDataFlow.Extensions;

namespace TplDataflow.Extensions.Example.Implementation
{
    /// <summary>
    /// Implements <see cref="IEventSetStorageProcessor" /> interface
    /// </summary>
    /// <seealso cref="IEventSetStorageProcessor" />
    public class EventSetStorageProcessor : IEventSetStorageProcessor
    {
        // TODO: Introduce configuration parameters
        private const int EventBatchSize = 1000;
        private const string EventBatchTimeout = "00:00:05";

        private const int EventGroupBatchSize = 25;
        private const string EventGroupBatchTimeout = "00:00:05";

        private const string EventSetSequenceName = "EventSet";
        private readonly Func<IEventSetRepository> _repositoryResolver;
        private readonly IIdentityManagementService _identityService;
        private readonly IEventSetProcessTypeManager _processTypeManager;
        private readonly IEventSetConfiguration _configuration;
        private readonly Func<DateTime> _currentTimeProvider;

        private readonly BufferBlock<EventDetails> _inputEventsBlock = new BufferBlock<EventDetails>();
        private readonly BufferBlock<EventSetWithEvents> _eventSetCreatedBlock = new BufferBlock<EventSetWithEvents>();
        private readonly BufferBlock<EventSetWithEvents> _eventSetUpdatedBlock = new BufferBlock<EventSetWithEvents>();
        private readonly BufferBlock<EventDetails> _eventSkippedBlock = new BufferBlock<EventDetails>();
        private readonly BufferBlock<EventDetails> _eventFailedBlock = new BufferBlock<EventDetails>();
        private readonly ILog _logger = LogManager.GetLogger(typeof(EventSetStorageProcessor));

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSetStorageProcessor" /> class.
        /// </summary>
        /// <param name="repositoryResolver">The repository resolver.</param>
        /// <param name="identityService">The identity service.</param>
        /// <param name="processTypeManager">The process type manager.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="currentTimeProvider">The current time provider.</param>
        /// <param name="isAsync">if set to <c>true</c> [is asynchronous].</param>
        public EventSetStorageProcessor(Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService, IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration, Func<DateTime> currentTimeProvider, bool isAsync = true)
        {
            _repositoryResolver = repositoryResolver;
            _identityService = identityService;
            _processTypeManager = processTypeManager;
            _configuration = configuration;
            _currentTimeProvider = currentTimeProvider;

            if (isAsync)
            {
                CreateDataflowAsync();
            }
            else
            {
                CreateDataflowSync();
            }
        }

        public IObserver<EventDetails> Input
        {
            get
            {
                return _inputEventsBlock.AsObserver();
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

        public Task CompletionTask
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

        private void CreateDataflowAsync()
        {
            _inputEventsBlock
                .SideEffect(LogEvent)
                .Buffer(TimeSpan.Parse(EventBatchTimeout), EventBatchSize)
                .SelectMany(SplitEventsIntoGroupsSafe)
                .SelectSafe(CheckNeedSkipEventGroup)
                .BufferSafe(TimeSpan.Parse(EventGroupBatchTimeout), EventGroupBatchSize)
                .SelectManySafe(ProcessEventGroupsBatchSafe)
                .Match(
                    success =>
                        success.Map(result => result.IsCreated,
                            resultCreated => resultCreated.Select(result => result.EventSetWithEvents)
                                .LinkWith(_eventSetCreatedBlock),
                            resultUpdated => resultUpdated.Select(result => result.EventSetWithEvents)
                                .LinkWith(_eventSetUpdatedBlock)),
                    failure => failure.Map(result => result.IsSkipped,
                        resultSkipped => resultSkipped.SelectMany(result => result.Events)
                            .LinkWith(_eventSkippedBlock),
                        resultFailed => resultFailed.SelectMany(result => result.Events)
                            .LinkWith(_eventFailedBlock)));
        }

        private void CreateDataflowSync()
        {
            _inputEventsBlock.AsObservable().ToEnumerable()
                .SideEffect(LogEvent)
                .Buffer(TimeSpan.Parse(EventBatchTimeout), EventBatchSize)
                .SelectMany(SplitEventsIntoGroupsSafe)
                .SelectSafe(CheckNeedSkipEventGroup)
                .BufferSafe(TimeSpan.Parse(EventGroupBatchTimeout), EventGroupBatchSize)
                .SelectManySafe(ProcessEventGroupsBatchSafe)
                .Match(
                    success =>
                        success.Map(result => result.IsCreated,
                            resultCreated => resultCreated.Select(result => result.EventSetWithEvents)
                                .LinkTo(_eventSetCreatedBlock.AsObserver()),
                            resultUpdated => resultUpdated.Select(result => result.EventSetWithEvents)
                                .LinkTo(_eventSetUpdatedBlock.AsObserver())),
                    failure => failure.Map(result => result.IsSkipped,
                        resultSkipped => resultSkipped.SelectMany(result => result.Events)
                            .LinkTo(_eventSkippedBlock.AsObserver()),
                        resultFailed => resultFailed.SelectMany(result => result.Events)
                            .LinkTo(_eventFailedBlock.AsObserver())));
        }

        private void LogEvent(EventDetails @event)
        {
            _logger.DebugFormat("EventSet processing started for event [EventId = {0}]", @event.Id);
        }

        private IEnumerable<EventGroup> SplitEventsIntoGroups(IList<EventDetails> events)
        {
            var eventGroupsByEventType = events
                .GroupBy(@event => new
                {
                    EventTypeId = @event.EventTypeId,
                    EventCategory = @event.Category
                })
                .Select(groups => groups);

            var processTypesWithEvents = eventGroupsByEventType.Select(eventGroup => new
            {
                EventSetProcessType = GetProcessType(eventGroup.Key.EventTypeId, eventGroup.Key.EventCategory),
                Events = eventGroup.ToList()
            });

            var eventInfos =
                processTypesWithEvents.SelectMany(processType => processType.Events,
                    (processType, @event) => new
                    {
                        Event = @event,
                        EventSetProcessType = processType.EventSetProcessType,
                        EventSetType = EventSetType.CreateFromEventAndLevel(@event, (EventLevel)processType.EventSetProcessType.Level)
                    });

            var eventGroupsByEventSetType = eventInfos
                .GroupBy(eventInfo => eventInfo.EventSetType)
                .Select(eventGroup => new EventGroup
                {
                    EventSetType = eventGroup.Key,
                    EventSetProcessType = eventGroup.First().EventSetProcessType,
                    Events = eventGroup.Select(arg => arg.Event).ToList()
                });

            return eventGroupsByEventSetType
                .SelectMany(SplitEventGroupByThreshold);
        }

        private IEnumerable<Result<EventGroup, UnsuccessResult>> SplitEventsIntoGroupsSafe(IList<EventDetails> events)
        {
            // TODO: Handle exceptions more specifically inside SplitEventsIntoGroups
            Exception exception = null;
            IEnumerable<EventGroup> successResult = null;
            try
            {
                successResult = SplitEventsIntoGroups(events).ToList();
            }
            catch (Exception e)
            {
                exception = e;
            }
            if (exception != null)
            {
                return Enumerable.Repeat(Result.Failure<EventGroup, UnsuccessResult>(UnsuccessResult.CreateError(events, Metadata.ExceptionHandling.UnhandledException.Code, exception.Message)), 1);
            }
            return successResult.ToResult<EventGroup, UnsuccessResult>();
        }

        private Result<EventGroup, UnsuccessResult> CheckNeedSkipEventGroup(EventGroup eventGroup)
        {
            if (NeedSkipEventGroup(eventGroup))
            {
                return Result.Failure<EventGroup, UnsuccessResult>(UnsuccessResult.CreateSkipped(eventGroup.Events));
            }
            return Result.Success<EventGroup, UnsuccessResult>(eventGroup);
        }

        private bool NeedSkipEventGroup(EventGroup eventGroup)
        {
            return eventGroup.EventSetType.Level == EventLevel.Information;
        }

        private IEnumerable<SuccessResult> ProcessEventGroupsBatch(IList<EventGroup> eventGroupsBatch)
        {
            using (var repository = _repositoryResolver())
            {
                long[] typeCodes = eventGroupsBatch
                    .Select(eventGroup => eventGroup.EventSetType.GetCode())
                    .Distinct()
                    .ToArray();
                IList<EventSet> lastEventSets = repository.FindLastEventSetsByTypeCodes(typeCodes);
                var results = eventGroupsBatch
                    .GroupBy(eventGroup => NeedToCreateEventSet(eventGroup, lastEventSets))
                    .SelectMany(eventGroup => eventGroup.Key
                        ? CreateEventSets(eventGroup.ToList())
                        : UpdateEventSets(eventGroup.ToList(), lastEventSets))
                    .ToList();
                ApplyChanges(repository, results);
                return results;
            }
        }

        private IEnumerable<Result<SuccessResult, UnsuccessResult>> ProcessEventGroupsBatchSafe(IList<EventGroup> eventGroupsBatch)
        {
            // TODO: Handle exceptions more specifically inside ProcessEventGroupsBatch
            Exception exception = null;
            IEnumerable<SuccessResult> successResult = null;
            try
            {
                successResult = ProcessEventGroupsBatch(eventGroupsBatch);
            }
            catch (Exception e)
            {
                exception = e;
            }
            if (exception != null)
            {
                return Enumerable.Repeat(Result.Failure<SuccessResult, UnsuccessResult>(
                    UnsuccessResult.CreateError(eventGroupsBatch.SelectMany(group => group.Events), Metadata.ExceptionHandling.UnhandledException.Code, exception.Message)), 1);
            }
            return successResult.ToResult<SuccessResult, UnsuccessResult>();
        }

        private static void ApplyChanges(IEventSetRepository repository, IList<SuccessResult> results)
        {
            repository.ApplyChanges(
                results
                    .Where(result => result.IsCreated)
                    .Select(result => result.EventSetWithEvents.EventSet)
                    .ToList(),
                results
                    .Where(result => !result.IsCreated)
                    .Select(result => result.EventSetWithEvents.EventSet)
                    .ToList());
        }

        private bool NeedToCreateEventSet(EventGroup eventGroup, IList<EventSet> lastEventSets)
        {
            EventSet lastEventSet = lastEventSets.FirstOrDefault(set => set.TypeCode == eventGroup.EventSetType.GetCode());
            return lastEventSet == null
                   || IsEventSetOutdated(lastEventSet, eventGroup.EventSetProcessType, eventGroup.Events.Min(@event => @event.ReadTime));
        }

        private bool IsEventSetOutdated(EventSet eventSet, EventSetProcessType eventSetProcessType, DateTime nextReadTime)
        {
            if (eventSet.Status == (int)EventSetStatus.Completed)
            {
                _logger.DebugFormat("EventSet is already completed [Id = {0}].", eventSet.Id);
                return true;
            }

            if (eventSetProcessType.Threshold.Ticks > 0 && nextReadTime - eventSet.LastReadTime >= eventSetProcessType.Threshold)
            {
                _logger.DebugFormat("EventSet outdated by threshold [Id = {0}, Threshold = {1}].",
                    eventSet.Id, eventSetProcessType.Threshold);
                return true;
            }

            if (eventSetProcessType.AutoComplete && eventSetProcessType.AutoCompleteTimeout.HasValue && ShouldBeCompletedByAutoComplete(eventSet, eventSetProcessType.AutoCompleteTimeout.Value))
            {
                _logger.DebugFormat("EventSet should be completed [Id = {0}, Current Status = {1}, AutoCompleteTimeout = {2}].",
                    eventSet.Id, eventSet.Status, eventSetProcessType.AutoCompleteTimeout);
                return true;
            }

            return false;
        }

        private bool ShouldBeCompletedByAutoComplete(EventSet eventSet, TimeSpan timeout)
        {
            DateTime autoCompleteTime = DateTime.UtcNow.Subtract(timeout);

            return (eventSet.Status == (byte)EventSetStatus.New && eventSet.CreationTime < autoCompleteTime)
                   ||
                   (
                       (eventSet.Status == (byte)EventSetStatus.Accepted || eventSet.Status == (byte)EventSetStatus.Rejected)
                       && eventSet.AcceptedTime < autoCompleteTime
                       );
        }

        private IList<SuccessResult> CreateEventSets(IList<EventGroup> eventGroups)
        {
            IList<long> ids = _identityService.GetNextLongIds(EventSetSequenceName, eventGroups.Count);

            if (ids.Count < eventGroups.Count)
            {
                throw new InvalidOperationException("Not all event set identifiers are generated by identity service.");
            }

            return eventGroups
                .Zip(ids, (eventGroup, eventSetId) =>
                    new { EventGroup = eventGroup, EventSetId = eventSetId })
                .Select(item => CreateEventSet(item.EventSetId, item.EventGroup))
                .ToList();
        }

        private SuccessResult CreateEventSet(long eventSetId, EventGroup eventGroup)
        {
            DateTime currentTime = _currentTimeProvider();
            var eventSet = new EventSet
            {
                Id = eventSetId,
                EventTypeId = eventGroup.EventSetType.EventTypeId,
                EventTypeCategory = (byte)eventGroup.EventSetType.EventTypeCategory,
                ResourceId = eventGroup.EventSetType.ResourceId,
                ResourceCategory = eventGroup.EventSetType.ResourceCategory,
                SiteId = eventGroup.EventSetType.SiteId,
                Level = (byte)eventGroup.EventSetType.Level,
                Status = (byte)EventSetStatus.New,
                CreationTime = currentTime,
                FirstReadTime = eventGroup.Events.Min(@event => @event.ReadTime),
                LastReadTime = eventGroup.Events.Max(@event => @event.ReadTime),
                LastUpdateTime = currentTime,
                EventsCount = eventGroup.Events.Count,
                TypeCode = eventGroup.EventSetType.GetCode()
            };

            _logger.DebugFormat("EventSet entity created [Id = {0}, EventTypeId = {1}, ResourceId = {2}, EventsCount = {3}, EventIds = {4}].",
                eventSet.Id, eventSet.EventTypeId, eventSet.ResourceId, eventGroup.Events.Count, string.Join(",", eventGroup.Events.Select(details => details.Id)));

            return new SuccessResult(isCreated: true, eventSet: eventSet, events: eventGroup.Events);
        }

        private IList<SuccessResult> UpdateEventSets(IList<EventGroup> eventGroups, IList<EventSet> lastEventSets)
        {
            return eventGroups
                .Select(eventGroup => UpdateEventSet(eventGroup, lastEventSets))
                .ToList();
        }

        private SuccessResult UpdateEventSet(EventGroup eventGroup, IList<EventSet> lastEventSets)
        {
            IList<EventDetails> events = eventGroup.Events;
            EventSet lastEventSet = lastEventSets.First(set => set.TypeCode == eventGroup.EventSetType.GetCode());

            lastEventSet.FirstReadTime = events
                .Select(@event => @event.ReadTime)
                .Concat(new[] { lastEventSet.FirstReadTime })
                .Min();
            lastEventSet.LastReadTime = events
                .Select(@event => @event.ReadTime)
                .Concat(new[] { lastEventSet.LastReadTime })
                .Max();
            lastEventSet.LastUpdateTime = _currentTimeProvider();
            lastEventSet.EventsCount += events.Count;

            _logger.DebugFormat("EventSet entity updated [Id = {0}, EventTypeId = {1}, ResourceId = {2}, EventsCountDelta = {3}, EventsCount = {4}, EventIds = {5}].",
                lastEventSet.Id, lastEventSet.EventTypeId, lastEventSet.ResourceId, events.Count, lastEventSet.EventsCount, string.Join(",", events.Select(@event => @event.Id)));

            return new SuccessResult(isCreated: false, eventSet: lastEventSet, events: eventGroup.Events);
        }

        private IEnumerable<EventGroup> SplitEventGroupByThreshold(EventGroup eventGroup)
        {
            TimeSpan threshold = eventGroup.EventSetProcessType.Threshold;

            IEnumerator<EventDetails> enumerator = eventGroup.Events
                .OrderBy(@event => @event.ReadTime)
                .GetEnumerator();
            enumerator.MoveNext();

            DateTime lastTime = enumerator.Current.ReadTime;
            var events = new List<EventDetails>();

            do
            {
                var newTime = enumerator.Current.ReadTime;

                if (threshold.Ticks > 0 && newTime - lastTime >= threshold)
                {
                    yield return new EventGroup
                    {
                        EventSetType = eventGroup.EventSetType,
                        EventSetProcessType = eventGroup.EventSetProcessType,
                        Events = events
                    };
                    events = new List<EventDetails>();
                }

                lastTime = newTime;
                events.Add(enumerator.Current);
            }
            while (enumerator.MoveNext());

            yield return new EventGroup
            {
                EventSetType = eventGroup.EventSetType,
                EventSetProcessType = eventGroup.EventSetProcessType,
                Events = events
            };
        }

        private EventDetails[] HandleSplitEventsIntoGroupsException(Tuple<Exception, EventDetails[]> tuple)
        {
            IList<EventDetails> events = tuple.Item2
                .ToList();
            _logger.ErrorFormat(tuple.Item1.Message, "Split events into groups failed [EventIds = {0}]",
                string.Join(",", events));
            return events.ToArray();
        }

        private EventDetails[] HandleEventGroupsBatchException(Tuple<Exception, EventGroup[]> tuple)
        {
            IList<EventDetails> events = tuple.Item2
                .SelectMany(eventGroup => eventGroup.Events)
                .ToList();
            _logger.ErrorFormat(tuple.Item1.Message, "Event group processing failed [EventIds = {0}]",
                string.Join(",", events.Select(@event => @event.Id)));
            return events.ToArray();
        }

        private EventSetProcessType GetProcessType(int eventTypeId, EventTypeCategory category)
        {
            var eventSetProcessType = _processTypeManager.GetProcessType(eventTypeId, category);
            if (eventSetProcessType == null)
            {
                throw EventHandlingException.CreateNotFoundException("EventSetProcessingType was not found for [EventTypeId = {0}, Category = {1}]",
                    eventTypeId, category);
            }

            return eventSetProcessType;
        }

        private class EventGroup
        {
            public EventSetType EventSetType { get; set; }

            public EventSetProcessType EventSetProcessType { get; set; }

            public List<EventDetails> Events { get; set; }
        }

        private class SuccessResult
        {
            private readonly bool _isCreated;
            private readonly EventSetWithEvents _eventSetWithEvents;

            public SuccessResult(bool isCreated, EventSet eventSet, IList<EventDetails> events)
            {
                _isCreated = isCreated;
                _eventSetWithEvents = new EventSetWithEvents { EventSet = eventSet, Events = events };
            }

            public bool IsCreated
            {
                get
                {
                    return _isCreated;
                }
            }

            public EventSetWithEvents EventSetWithEvents
            {
                get
                {
                    return _eventSetWithEvents;
                }
            }
        }

        private class UnsuccessResult
        {
            private readonly bool _isSkipped;
            private readonly IEnumerable<EventDetails> _events;
            private readonly int _errorCode;
            private readonly string _errorMessage;

            private UnsuccessResult(bool isSkipped, IEnumerable<EventDetails> events, int errorCode, string errorMessage)
            {
                _isSkipped = isSkipped;
                _events = events;
                _errorCode = errorCode;
                _errorMessage = errorMessage;
            }

            public bool IsSkipped
            {
                get
                {
                    return _isSkipped;
                }
            }

            public IEnumerable<EventDetails> Events
            {
                get
                {
                    return _events;
                }
            }

            public int ErrorCode
            {
                get
                {
                    return _errorCode;
                }
            }

            public string ErrorMessage
            {
                get
                {
                    return _errorMessage;
                }
            }

            public static UnsuccessResult CreateError(IEnumerable<EventDetails> events, int errorCode, string errorMessage)
            {
                return new UnsuccessResult(false, events, errorCode, errorMessage);
            }

            public static UnsuccessResult CreateSkipped(IEnumerable<EventDetails> events)
            {
                return new UnsuccessResult(true, events, 0, string.Empty);
            }
        }
    }
}