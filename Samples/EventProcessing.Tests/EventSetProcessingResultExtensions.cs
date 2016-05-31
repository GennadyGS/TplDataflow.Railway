using EventProcessing.BusinessObjects;
using EventProcessing.Implementation;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventProcessing.Tests
{
    public static class EventSetProcessingResultExtensions
    {
        public static void VerifyEventSkipped(this EventSetStorageProcessor.Result result,
            EventDetails @event)
        {
            result.ResultCode.Should().Be(EventSetStorageProcessor.ResultCode.EventSkipped);
            result.EventSkipped.Should().Be(@event);
        }

        public static void VerifyEventFailed(this EventSetStorageProcessor.Result result,
            EventDetails @event)
        {
            result.ResultCode.Should().Be(EventSetStorageProcessor.ResultCode.EventFailed);
            result.EventFailed.Should().Be(@event);
        }

        public static bool VerifyEventSetCreated(this EventSetStorageProcessor.Result result, 
            long eventSetId, EventDetails sourceEvent, EventLevel level, DateTime currentTime)
        {
            return result.ResultCode == EventSetStorageProcessor.ResultCode.EventSetCreated
                   && result.EventSetCreated.EventSet.VerifyCreatedEventSet(eventSetId, sourceEvent, level, currentTime);
        }

        public static void VerifyEventSetsCreatedForEachEvent(this IList<EventSetStorageProcessor.Result> results, 
            IList<long> eventSetIds, IList<EventDetails> sourceEvents, EventLevel level, DateTime currentTime)
        {
            results.Should()
                .HaveCount(sourceEvents.Count, "Expected that {0} event set should be created while actually {1}.",
                    sourceEvents.Count, sourceEvents.Count)
                .And.OnlyContain(result => result.ResultCode == EventSetStorageProcessor.ResultCode.EventSetCreated);
                
            results.Select(result => result.EventSetCreated)
                .Zip(sourceEvents, (eventSetWithEvents, sourceEvent) =>
                    new { EventSetWithEvents = eventSetWithEvents, SourceEvent = sourceEvent })
                .Zip(eventSetIds, (item, eventSetId) =>
                    new { EventSetId = eventSetId, EventSetWithEvents = item.EventSetWithEvents, SourceEvent = item.SourceEvent })
                .ToList()
                .ForEach(item => item.EventSetWithEvents.EventSet.VerifyCreatedEventSet(item.EventSetId, item.SourceEvent, level, currentTime));
        }

        public static bool VerifyEventSetUpdated(this EventSetStorageProcessor.Result result,
            EventDetails sourceEvent, int expectedEventCount, DateTime currentTime)
        {
            return result.ResultCode == EventSetStorageProcessor.ResultCode.EventSetUpdated
                   && result.EventSetUpdated.EventSet.VerifyUpdatedEventSet(sourceEvent, expectedEventCount, currentTime);
        }

        private static bool VerifyCreatedEventSet(this EventSet eventSet, long eventSetId, EventDetails sourceEvent, EventLevel level, DateTime currentTime)
        {
            eventSet.Id.Should().Be(eventSetId);
            eventSet.Level.Should().Be((byte)level);
            eventSet.EventTypeId.Should().Be(sourceEvent.EventTypeId);
            eventSet.ResourceCategory.Should().Be(sourceEvent.ResourceCategory);
            eventSet.ResourceId.Should().Be(sourceEvent.ResourceId);
            eventSet.SiteId.Should().Be(sourceEvent.SiteId);
            eventSet.EventsCount.Should().Be(1);
            eventSet.LastReadTime.Should().Be(sourceEvent.ReadTime);
            eventSet.LastUpdateTime.Should().Be(currentTime);
            eventSet.CreationTime.Should().Be(currentTime);
            return true;
        }

        private static bool VerifyUpdatedEventSet(this EventSet eventSet, EventDetails sourceEvent, int expectedEventCount, DateTime currentTime)
        {
            eventSet.EventsCount.Should().Be(expectedEventCount);
            eventSet.LastReadTime.Should().Be(sourceEvent.ReadTime);
            eventSet.LastUpdateTime.Should().Be(currentTime);
            return true;
        }
    }
}
