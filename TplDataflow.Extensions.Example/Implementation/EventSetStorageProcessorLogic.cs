using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
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
    internal class EventSetStorageProcessorLogic
    {
        private const string EventSetSequenceName = "EventSet";

        private readonly Func<IEventSetRepository> _repositoryResolver;
        private readonly IIdentityManagementService _identityService;
        private readonly IEventSetProcessTypeManager _processTypeManager;
        private readonly Func<DateTime> _currentTimeProvider;

        private readonly ILog _logger = LogManager.GetLogger(typeof(EventSetStorageProcessorLogic));

        public EventSetStorageProcessorLogic(Func<IEventSetRepository> repositoryResolver, IIdentityManagementService identityService, IEventSetProcessTypeManager processTypeManager, Func<DateTime> currentTimeProvider)
        {
            _repositoryResolver = repositoryResolver;
            _identityService = identityService;
            _processTypeManager = processTypeManager;
            _currentTimeProvider = currentTimeProvider;
        }

        private static Result<T, UnsuccessResult> InvokeSafe<T>(
            IEnumerable<EventDetails> events, Func<Result<T, UnsuccessResult>> func)
        {
            try
            {
                return func();
            }
            catch (EventHandlingException e)
            {
                return Result.Failure<T, UnsuccessResult>(
                    UnsuccessResult.CreateError(events, e.ErrorCode, e.Message));
            }
            catch (DBConcurrencyException e)
            {
                return Result.Failure<T, UnsuccessResult>(
                    UnsuccessResult.CreateError(events, Metadata.ExceptionHandling.DbUpdateConcurrencyException.Code, e.Message));
            }
            catch (Exception e)
            {
                return Result.Failure<T, UnsuccessResult>(
                    UnsuccessResult.CreateError(events, Metadata.ExceptionHandling.UnhandledException.Code, e.Message));
            }
        }

        private static Result<T, UnsuccessResult> InvokeSafe<T>(
            IEnumerable<EventDetails> events, Func<T> func)
        {
            return InvokeSafe(events, () => func().ToResult<T, UnsuccessResult>());
        }

        public EventDetails LogEvent(EventDetails @event)
        {
            _logger.DebugFormat("EventSet processing started for event [EventId = {0}]", @event.Id);
            return @event;
        }

        public IEnumerable<Result<EventGroup, UnsuccessResult>> SplitEventsIntoGroupsSafe(IList<EventDetails> events)
        {
            var eventGroupsByEventType = events
                .GroupBy(@event => new
                {
                    EventTypeId = @event.EventTypeId,
                    EventCategory = @event.Category
                });

            var processTypesWithEvents = eventGroupsByEventType
                .Select(eventGroup => GetProcessTypeSafe(eventGroup.Key.EventTypeId, eventGroup.Key.EventCategory, eventGroup)
                    .Select(processType => new
                    {
                        EventSetProcessType = processType,
                        Events = eventGroup
                    }));

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

        public Result<EventGroup, UnsuccessResult> CheckNeedSkipEventGroup(EventGroup eventGroup)
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

        public IEnumerable<Result<SuccessResult, UnsuccessResult>> ProcessEventGroupsBatchSafe(IList<EventGroup> eventGroupsBatch)
        {
            using (var repository = _repositoryResolver())
            {
                var events = eventGroupsBatch
                    .SelectMany(eventGroup => eventGroup.Events)
                    .ToList();
                return FindLastEventSetsSafe(repository, eventGroupsBatch, events)
                    .SelectManySafe(lastEventSets => InternalProcessEventGroupsBatchSafe(repository, eventGroupsBatch, lastEventSets))
                    .ToList()
                    .SelectSafe(resultList => ApplyChangesSafe(repository, resultList))
                    .SelectMany(result => result);
            }
        }

        private static Result<IList<EventSet>, UnsuccessResult> FindLastEventSetsSafe(IEventSetRepository repository,
            IList<EventGroup> eventGroupsBatch, IList<EventDetails> events)
        {
            long[] typeCodes = eventGroupsBatch
                .Select(eventGroup => eventGroup.EventSetType.GetCode())
                .Distinct()
                .ToArray();
            return InvokeSafe(events, () => repository.FindLastEventSetsByTypeCodes(typeCodes));
        }

        private IEnumerable<Result<SuccessResult, UnsuccessResult>> InternalProcessEventGroupsBatchSafe(IEventSetRepository repository, IList<EventGroup> eventGroupsBatch, IList<EventSet> lastEventSets)
        {
            return eventGroupsBatch
                .GroupBy(eventGroup => NeedToCreateEventSet(eventGroup, lastEventSets))
                .SelectMany(eventGroup => eventGroup.Key
                    ? CreateEventSets(eventGroup.ToList())
                    : UpdateEventSets(eventGroup.ToList(), lastEventSets));
        }

        private static Result<IList<SuccessResult>, UnsuccessResult> ApplyChangesSafe(IEventSetRepository repository, IList<SuccessResult> results)
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
            return results.ToResult<IList<SuccessResult>, UnsuccessResult>();
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

        private IEnumerable<Result<SuccessResult, UnsuccessResult>> CreateEventSets(IList<EventGroup> eventGroups)
        {
            return GenerateEventSetIdsSafe(eventGroups)
                .SelectMany(eventSetIds =>
                    eventGroups
                        .Zip(eventSetIds, (eventGroup, eventSetId) =>
                            new { EventGroup = eventGroup, EventSetId = eventSetId })
                        .Select(item => CreateEventSet(item.EventSetId, item.EventGroup)));
        }

        private Result<IList<long>, UnsuccessResult> GenerateEventSetIdsSafe(IList<EventGroup> eventGroups)
        {
            var events = eventGroups
                .SelectMany(group => group.Events)
                .ToList();

            return InvokeSafe(events, () => GenerateEventSetIds(eventGroups));
        }

        private IList<long> GenerateEventSetIds(IList<EventGroup> eventGroups)
        {
            IList<long> ids = _identityService.GetNextLongIds(EventSetSequenceName, eventGroups.Count);

            if (ids.Count < eventGroups.Count)
            {
                throw new InvalidOperationException(
                    "Not all event set identifiers are generated by identity service.");
            }

            return ids;
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

        private IEnumerable<Result<SuccessResult, UnsuccessResult>> UpdateEventSets(IList<EventGroup> eventGroups, IList<EventSet> lastEventSets)
        {
            return eventGroups
                .Select(eventGroup => UpdateEventSet(eventGroup, lastEventSets))
                .ToResult<SuccessResult, UnsuccessResult>();
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

        private Result<EventSetProcessType, UnsuccessResult> GetProcessTypeSafe(int eventTypeId, EventTypeCategory category, IEnumerable<EventDetails> events)
        {
            return InvokeSafe(events, () =>
                _processTypeManager.GetProcessType(eventTypeId, category) ??
                    Result.Failure<EventSetProcessType, UnsuccessResult>(
                        UnsuccessResult.CreateError(events,
                            Metadata.ExceptionHandling.NotFoundException.Code,
                            "EventSetProcessingType was not found for [EventTypeId = {0}, Category = {1}]", eventTypeId, category)));
        }

        internal class EventGroup
        {
            public EventSetType EventSetType { get; set; }

            public EventSetProcessType EventSetProcessType { get; set; }

            public List<EventDetails> Events { get; set; }
        }

        internal class SuccessResult
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

        internal class UnsuccessResult
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

            public static UnsuccessResult CreateError(IEnumerable<EventDetails> events, int errorCode, string errorMessageFormat, params object[] args)
            {
                return new UnsuccessResult(false, events, errorCode, string.Format(errorMessageFormat, args));
            }

            public static UnsuccessResult CreateSkipped(IEnumerable<EventDetails> events)
            {
                return new UnsuccessResult(true, events, 0, string.Empty);
            }
        }
     }
}