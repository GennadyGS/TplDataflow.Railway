// ==========================================================
//  Title: Central.Implementation
//  Description: Processes EventSet based on new upcoming events.
//  Copyright © 2004-2014 Modular Mining Systems, Inc.
//  All Rights Reserved
// ==========================================================
//  The information described in this document is furnished as proprietary
//  information and may not be copied or sold without the written permission
//  of Modular Mining Systems, Inc.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
using TplDataflow.Extensions.Example.BusinessObjects;
using TplDataflow.Extensions.Example.Exceptions;
using TplDataflow.Extensions.Example.Interfaces;
using TplDataflow.Extensions.Example.Utils;
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

        private readonly IObserver<EventDetails> _inputEventsBlock;
        private readonly JointPointBlock<EventSetWithEvents> _eventSetCreatedBlock = new JointPointBlock<EventSetWithEvents>();
        private readonly JointPointBlock<EventSetWithEvents> _eventSetUpdatedBlock = new JointPointBlock<EventSetWithEvents>();
        private readonly JointPointBlock<EventDetails> _eventSkippedBlock = new JointPointBlock<EventDetails>();
        private readonly JointPointBlock<EventDetails> _eventFailedBlock = new JointPointBlock<EventDetails>();
        private readonly ILog _logger = LogManager.GetLogger(typeof(EventSetStorageProcessor));

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSetStorageProcessor"/> class.
        /// </summary>
        /// <param name="repositoryResolver">The repository resolver.</param>
        /// <param name="identityService">The identity service.</param>
        /// <param name="processTypeManager">The process type manager.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="currentTimeProvider">The current time provider.</param>
        public EventSetStorageProcessor(Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService, IEventSetProcessTypeManager processTypeManager, IEventSetConfiguration configuration, Func<DateTime> currentTimeProvider)
        {
            _repositoryResolver = repositoryResolver;
            _identityService = identityService;
            _processTypeManager = processTypeManager;
            _configuration = configuration;
            _currentTimeProvider = currentTimeProvider;

            _inputEventsBlock = CreateDataflowAsync();
        }

        public IObserver<EventDetails> Input
        {
            get
            {
                return _inputEventsBlock;
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

        private IObserver<EventDetails> CreateDataflowAsync()
        {
            return DataflowBlockFactory.CreateSideEffectBlock<EventDetails>(@event =>
                    _logger.DebugFormat("EventSet processing started for event [EventId = {0}]", @event.Id))
                .CombineWith(DataflowBlockFactory.CreateTimedBatchBlock<EventDetails>(EventBatchSize, TimeSpan.Parse(EventBatchTimeout)))
                .CombineWith(new SafeTransformManyBlock<EventDetails[], EventGroup>(events => SplitEventsIntoGroups(events))
                    .HandleExceptionWith(
                        new TransformManyBlock<Tuple<Exception, EventDetails[]>, EventDetails>(item => HandleSplitEventsIntoGroupsException(item))
                            .LinkWith(_eventFailedBlock.AddInput())))
                .LinkWhen(NeedSkipEventGroup,
                    new TransformManyBlock<EventGroup, EventDetails>(eventGroup => eventGroup.Events)
                        .LinkWith(_eventSkippedBlock.AddInput()))
                .LinkOtherwise(
                    DataflowBlockFactory.CreateTimedBatchBlock<EventGroup>(EventGroupBatchSize, TimeSpan.Parse(EventGroupBatchTimeout))
                        .CombineWith(new SafeTransformBlock<EventGroup[], SuccessResult>(eventGroupBatch => ProcessEventGroupsBatchAsync(eventGroupBatch))
                            .HandleExceptionWith(
                                new TransformManyBlock<Tuple<Exception, EventGroup[]>, EventDetails>(item => HandleEventGroupsBatchException(item))
                                    .LinkWith(_eventFailedBlock.AddInput())))
                        .LinkWith(new ForkManyBlock<SuccessResult, EventSetWithEvents, EventSetWithEvents>(result => SplitProcessingResult(result))
                            .ForkTo(_eventSetCreatedBlock.AddInput(), _eventSetUpdatedBlock.AddInput())))
                .AsObserver();
        }

        private IObserver<EventDetails> CreateDataflowSync()
        {
            var res2 = new Subject<EventDetails>().ToEnumerable()
                .Buffer(TimeSpan.Parse(EventBatchTimeout), EventBatchSize)
                .ToResult<IList<EventDetails>, UnsuccessResult>()
                .SelectMany(SplitEventsIntoGroupsSafe);
            return new Subject<EventDetails>();
            return DataflowBlockFactory.CreateSideEffectBlock<EventDetails>(@event =>
                    _logger.DebugFormat("EventSet processing started for event [EventId = {0}]", @event.Id))
                .CombineWith(DataflowBlockFactory.CreateTimedBatchBlock<EventDetails>(EventBatchSize, TimeSpan.Parse(EventBatchTimeout)))
                .CombineWith(new SafeTransformManyBlock<EventDetails[], EventGroup>(events => SplitEventsIntoGroups(events))
                    .HandleExceptionWith(
                        new TransformManyBlock<Tuple<Exception, EventDetails[]>, EventDetails>(item => HandleSplitEventsIntoGroupsException(item))
                            .LinkWith(_eventFailedBlock.AddInput())))
                .LinkWhen(NeedSkipEventGroup,
                    new TransformManyBlock<EventGroup, EventDetails>(eventGroup => eventGroup.Events)
                        .LinkWith(_eventSkippedBlock.AddInput()))
                .LinkOtherwise(
                    DataflowBlockFactory.CreateTimedBatchBlock<EventGroup>(EventGroupBatchSize, TimeSpan.Parse(EventGroupBatchTimeout))
                        .CombineWith(new SafeTransformBlock<EventGroup[], SuccessResult>(eventGroupBatch => ProcessEventGroupsBatchAsync(eventGroupBatch))
                            .HandleExceptionWith(
                                new TransformManyBlock<Tuple<Exception, EventGroup[]>, EventDetails>(item => HandleEventGroupsBatchException(item))
                                    .LinkWith(_eventFailedBlock.AddInput())))
                        .LinkWith(new ForkManyBlock<SuccessResult, EventSetWithEvents, EventSetWithEvents>(result => SplitProcessingResult(result))
                            .ForkTo(_eventSetCreatedBlock.AddInput(), _eventSetUpdatedBlock.AddInput())))
                .AsObserver();
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

        private Result<IEnumerable<EventGroup>, UnsuccessResult> SplitEventsIntoGroupsSafe(IList<EventDetails> events)
        {
            return Result.Success<IEnumerable<EventGroup>, UnsuccessResult>(SplitEventsIntoGroups(events));
        }

        private bool NeedSkipEventGroup(EventGroup eventGroup)
        {
            return eventGroup.EventSetType.Level == EventLevel.Information;
        }

        private async Task<SuccessResult> ProcessEventGroupsBatchAsync(EventGroup[] eventGroupsBatch)
        {
            using (var repository = _repositoryResolver())
            {
                long[] typeCodes = eventGroupsBatch
                    .Select(eventGroup => eventGroup.EventSetType.GetCode())
                    .Distinct()
                    .ToArray();
                IList<EventSet> lastEventSets = await FindLastEventSetsByTypeCodesAsync(repository, typeCodes);
                var result = await eventGroupsBatch
                    .GroupBy(eventGroup => NeedToCreateEventSet(eventGroup, lastEventSets))
                    .Select(async group => @group.Key
                        ? await CreateEventSetsAsync(@group.ToList())
                        : UpdateEventSets(@group.ToList(), lastEventSets))
                    .Aggregate(SuccessResult.CombineAsync);
                await ApplyChangesAsync(repository, result);
                return result;
            }
        }

        private static Task ApplyChangesAsync(IEventSetRepository repository, SuccessResult result)
        {
            return Task.Run(() => repository.ApplyChanges(
                result.EventSetsCreated.ToList(), result.EventSetsUpdated.ToList()));
        }

        private static Task<IList<EventSet>> FindLastEventSetsByTypeCodesAsync(IEventSetRepository repository, long[] typeCodes)
        {
            return Task.Run(() => repository.FindLastEventSetsByTypeCodes(typeCodes));
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

        private async Task<SuccessResult> CreateEventSetsAsync(IList<EventGroup> eventGroups)
        {
            IList<long> ids = await GetNextLongIdsAsync(eventGroups);

            if (ids.Count < eventGroups.Count)
            {
                throw new InvalidOperationException("Not all event set identifiers are generated by identity service.");
            }

            return eventGroups
                 .Zip(ids, (eventGroup, eventSetId) =>
                    new { EventGroup = eventGroup, EventSetId = eventSetId })
                .Select(item => CreateEventSet(item.EventSetId, item.EventGroup))
                .Aggregate(SuccessResult.Combine);
        }

        private Task<IList<long>> GetNextLongIdsAsync(IList<EventGroup> eventGroups)
        {
            return Task.Run(() => _identityService.GetNextLongIds(EventSetSequenceName, eventGroups.Count));
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
            var eventSetProcessingResult = SuccessResult.CreateWithEventSetCreated(eventSet, eventGroup.Events);
            return eventSetProcessingResult;
        }

        private SuccessResult UpdateEventSets(IList<EventGroup> eventGroups, IList<EventSet> lastEventSets)
        {
            return eventGroups
                .Select(eventGroup => UpdateEventSet(eventGroup, lastEventSets))
                .Aggregate(SuccessResult.Combine);
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

            return SuccessResult.CrewteWithEventSetUpdated(lastEventSet, eventGroup.Events);
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

        private static Tuple<IEnumerable<EventSetWithEvents>, IEnumerable<EventSetWithEvents>> SplitProcessingResult(SuccessResult result)
        {
            return new Tuple<IEnumerable<EventSetWithEvents>, IEnumerable<EventSetWithEvents>>(
                result.EventSetsWithEventsCreated,
                result.EventSetsWithEventsUpdated);
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

        private enum InSuccessReason
        {
            Skipped,
            Failed
        }

        private class SuccessResult
        {
            private readonly IEnumerable<EventSetWithEvents> _eventSetsWithEventsCreated;
            private readonly IEnumerable<EventSetWithEvents> _eventSetsWithEventsUpdated;

            private SuccessResult(IList<EventSetWithEvents> eventSetsWithEventsCreated,
                IList<EventSetWithEvents> eventSetsWithEventsUpdated)
            {
                _eventSetsWithEventsCreated = eventSetsWithEventsCreated;
                _eventSetsWithEventsUpdated = eventSetsWithEventsUpdated;
            }

            public IEnumerable<EventSetWithEvents> EventSetsWithEventsCreated
            {
                get
                {
                    return _eventSetsWithEventsCreated;
                }
            }

            public IEnumerable<EventSet> EventSetsCreated
            {
                get
                {
                    return _eventSetsWithEventsCreated
                        .Select(eventSetWithEvents => eventSetWithEvents.EventSet);
                }
            }

            public IEnumerable<EventSetWithEvents> EventSetsWithEventsUpdated
            {
                get
                {
                    return _eventSetsWithEventsUpdated;
                }
            }

            public IEnumerable<EventSet> EventSetsUpdated
            {
                get
                {
                    return _eventSetsWithEventsUpdated
                        .Select(eventSetWithEvents => eventSetWithEvents.EventSet);
                }
            }

            public static SuccessResult CreateWithEventSetCreated(EventSet eventSet, IList<EventDetails> events)
            {
                return new SuccessResult(
                    new[]
                    {
                        new EventSetWithEvents
                        {
                            EventSet = eventSet,
                            Events = events
                        }
                    }.ToList(),
                    new EventSetWithEvents[] { });
            }

            public static SuccessResult CrewteWithEventSetUpdated(EventSet eventSet, IList<EventDetails> events)
            {
                return new SuccessResult(
                    new List<EventSetWithEvents>(),
                    new[]
                    {
                        new EventSetWithEvents
                        {
                            EventSet = eventSet,
                            Events = events
                        }
                    }.ToList());
            }

            public static SuccessResult Combine(SuccessResult result1, SuccessResult result2)
            {
                return new SuccessResult(
                    result1._eventSetsWithEventsCreated.Concat(result2._eventSetsWithEventsCreated).ToList(),
                    result1._eventSetsWithEventsUpdated.Concat(result2._eventSetsWithEventsUpdated).ToList());
            }

            public static async Task<SuccessResult> CombineAsync(Task<SuccessResult> result1, Task<SuccessResult> result2)
            {
                return Combine(await result1, await result2);
            }
        }

        private class UnsuccessResult
        {
            private readonly IEnumerable<EventDetails> _events;
            private readonly InSuccessReason _reason;
            private readonly string _message;

            public UnsuccessResult(IEnumerable<EventDetails> events, InSuccessReason reason, string message = null)
            {
                _events = events;
                _reason = reason;
                _message = message;
            }

            private IEnumerable<EventDetails> Events
            {
                get
                {
                    return _events;
                }
            }

            public InSuccessReason Reason
            {
                get
                {
                    return _reason;
                }
            }

            public string Message
            {
                get
                {
                    return _message;
                }
            }
        }
    }
}