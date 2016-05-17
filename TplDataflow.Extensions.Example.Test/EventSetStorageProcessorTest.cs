// ==========================================================
//  Title: Central.Implementation.Test
//  Description: Test for EventSetProcessTypeRepository.
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;
using TplDataflow.Extensions.Example.BusinessObjects;
using TplDataflow.Extensions.Example.Implementation;
using TplDataflow.Extensions.Example.Interfaces;

namespace TplDataflow.Extensions.Example.Test
{
    [TestClass]
    public class EventSetStorageProcessorTest
    {
        private IEventSetRepository _repositoryMock;
        private IIdentityManagementService _identityManagementServiceMock;
        private IEventSetProcessTypeManager _processTypeManagerMock;
        private IEventSetConfiguration _configurationMock;

        private IEventSetStorageProcessor _storageProcessor;

        private IList<EventSetWithEvents> _storageProcessorEventSetCreatedOutput;
        private IList<EventSetWithEvents> _storageProcessorEventSetUpdatedOutput;
        private IList<EventDetails> _storageProcessorEventSkippedOutput;
        private IList<EventDetails> _storageProcessorEventFailedOutput;

        private readonly DateTime _currentTime = DateTime.UtcNow;

        private readonly EventDetails _criticalEvent;
        private readonly EventDetails _informationalEvent;

        public EventSetStorageProcessorTest()
        {
            _criticalEvent = new EventDetails
            {
                Category = EventTypeCategory.OemEvent,
                EventTypeId = 1,
                ResourceCategory = 2,
                ResourceId = 3,
                ReadTime = _currentTime,
                SiteId = 12
            };

            _informationalEvent = new EventDetails
            {
                Category = EventTypeCategory.OemEvent,
                EventTypeId = 2,
                ResourceCategory = 2,
                ResourceId = 3,
                ReadTime = _currentTime,
                SiteId = 12
            };

        }

        [TestInitialize]
        public void TestSetup()
        {
            _repositoryMock = Mock.Create<IEventSetRepository>();
            _identityManagementServiceMock = Mock.Create<IIdentityManagementService>();
            _processTypeManagerMock = Mock.Create<IEventSetProcessTypeManager>();
            _configurationMock = Mock.Create<IEventSetConfiguration>();

            _storageProcessor = new EventSetStorageProcessor(() => _repositoryMock, _identityManagementServiceMock, _processTypeManagerMock, _configurationMock, () => _currentTime);
            _storageProcessorEventSetCreatedOutput = _storageProcessor.EventSetCreatedOutput.CreateList();
            _storageProcessorEventSetUpdatedOutput = _storageProcessor.EventSetUpdatedOutput.CreateList();
            _storageProcessorEventSkippedOutput = _storageProcessor.EventSkippedOutput.CreateList();
            _storageProcessorEventFailedOutput = _storageProcessor.EventFailedOutput.CreateList();
        }

        [TestMethod]
        public void WhenInformationalEventOccured_EventShouldBeSkipped()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Information };

            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_informationalEvent.EventTypeId),
                    Arg.Is(_informationalEvent.Category)))
                .Returns(processType);

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(Arg.IsAny<long[]>()))
                .OccursNever();

            Mock.Arrange(() => _repositoryMock.ApplyChanges(Arg.IsAny<IList<EventSet>>(), Arg.IsAny<IList<EventSet>>()))
                .OccursNever();

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), Arg.IsAny<int>()))
                .OccursNever();

            ProcessEvents(new[] { _informationalEvent });

            _storageProcessorEventSetCreatedOutput.Should().BeEmpty();
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.ShouldAllBeEquivalentTo(new[] { _informationalEvent });
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenEventSetProcessTypeWasNotFound_EventShouldBeFailed()
        {
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(Arg.AnyInt, Arg.IsAny<EventTypeCategory>()))
                .Returns((EventSetProcessType)null);

            ProcessEvents(new[] { _informationalEvent });

            _storageProcessorEventSetCreatedOutput.Should().BeEmpty();
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.ShouldAllBeEquivalentTo(new[] { _informationalEvent });
        }

        [TestMethod]
        public void WhenEventOccurred_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Critical };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                    Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new List<EventSet>())
                .OccursOnce();

            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0)))
                .OccursOnce();

            long newEventSetId = 15345;

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), 1))
                .Returns(new[] { newEventSetId }.ToList())
                .OccursOnce();

            ProcessEvents(new[] { _criticalEvent });

            VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical);
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenEventSetWasFoundInDb_ItShouldBeUpdated()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Critical, Threshold = TimeSpan.FromHours(1) };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                Level = (byte)EventLevel.Critical,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                    Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();

            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1)))
                .OccursOnce();

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), Arg.IsAny<int>()))
                .OccursNever();

            ProcessEvents(new[] { _criticalEvent });

            _storageProcessorEventSetCreatedOutput.Should().BeEmpty();
            VerifyEventSetUpdated(_criticalEvent, 1);
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenEventSetIsOutdated_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Critical };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                Status = (byte)EventSetStatus.Completed,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                    Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();
            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0)))
                .OccursOnce();

            long newEventSetId = 15345;

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), 1))
                .Returns(new[] { newEventSetId }.ToList())
                .OccursOnce();

            ProcessEvents(new[] { _criticalEvent });

            VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical);
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenThresholdOccurred_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10)
            };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-11),
                TypeCode = GetCriticalEventsetTypeCode()
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                    Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();
            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0)))
                .OccursOnce();

            long newEventSetId = 15345;

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), 1))
                .Returns(new[] { newEventSetId }.ToList())
                .OccursOnce();

            ProcessEvents(new[] { _criticalEvent });

            VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical);
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenCurrentEventSetCompleted_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10)
            };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                Status = (byte)EventSetStatus.Completed,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                    Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();
            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0)))
                .OccursOnce();

            long newEventSetId = 15345;

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), 1))
                .Returns(new[] { newEventSetId }.ToList())
                .OccursOnce();

            ProcessEvents(new[] { _criticalEvent });

            VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical);
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenCurrentAutocompleteTimeOccurredAndStatusIsNew_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10),
                AutoComplete = true,
                AutoCompleteTimeout = TimeSpan.FromSeconds(9)
            };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                CreationTime = _currentTime.AddMinutes(-1),
                Status = (byte)EventSetStatus.New,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                    Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();
            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0)))
                .OccursOnce();

            long newEventSetId = 15345;

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), 1))
                .Returns(new[] { newEventSetId }.ToList())
                .OccursOnce();

            ProcessEvents(new[] { _criticalEvent });

            VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical);
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenCurrentAutocompleteTimeOccurredAndStatusIsAccepted_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10),
                AutoComplete = true,
                AutoCompleteTimeout = TimeSpan.FromSeconds(9)
            };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                AcceptedTime = _currentTime.AddMinutes(-1),
                Status = (byte)EventSetStatus.Accepted,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                    Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();
            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0)))
                .OccursOnce();

            long newEventSetId = 15345;

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), 1))
                .Returns(new[] { newEventSetId }.ToList())
                .OccursOnce();

            ProcessEvents(new[] { _criticalEvent });

            VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical);
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenCurrentAutocompleteTimeOccurredAndStatusIsRejected_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10),
                AutoComplete = true,
                AutoCompleteTimeout = TimeSpan.FromSeconds(9)
            };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                    Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                AcceptedTime = _currentTime.AddMinutes(-1),
                Status = (byte)EventSetStatus.Rejected,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                    Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();
            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0)))
                .OccursOnce();

            long newEventSetId = 15345;

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), 1))
                .Returns(new[] { newEventSetId }.ToList())
                .OccursOnce();

            ProcessEvents(new[] { _criticalEvent });

            VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical);
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenThresholdOccurredBetweenEvents_NewEventSetsForEachEventShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(5)
            };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-12),
                TypeCode = GetCriticalEventsetTypeCode()
            };

            var events = new[]
            {
                _criticalEvent
                    .Clone()
                    .WithReadTime(_currentTime.AddMinutes(-6)),
                _criticalEvent
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();
            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == events.Length),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 0)))
                .OccursOnce();

            var eventSetIds = new long[] { 15345, 15346 };
            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), eventSetIds.Length))
                .Returns(eventSetIds.ToList())
                .OccursOnce();

            ProcessEvents(events);

            VerifyEventSetsCreatedForEachEvent(eventSetIds, events, EventLevel.Critical);
            _storageProcessorEventSetUpdatedOutput.Should().BeEmpty();
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        [TestMethod]
        public void WhenThresholdOccurredBeforeSecondEvent_EventSetShouldBeUpdatedAndNewEventSetsShouldBeCreatedForSecondEvent()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(5)
            };
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(
                Arg.Is(_criticalEvent.EventTypeId), Arg.Is(_criticalEvent.Category)))
                .Returns(processType);

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-7),
                TypeCode = GetCriticalEventsetTypeCode()
            };
            var events = new[]
            {
                _criticalEvent
                    .Clone()
                    .WithReadTime(_currentTime.AddMinutes(-6)),
                _criticalEvent
            };

            Mock.Arrange(() => _repositoryMock.FindLastEventSetsByTypeCodes(
                Arg.Matches<long[]>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(new[] { eventSet }.ToList())
                .OccursOnce();
            Mock.Arrange(() => _repositoryMock.ApplyChanges(
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1),
                    Arg.Matches<IList<EventSet>>(sets => sets.Count == 1)))
                .OccursOnce();

            long newEventSetId = 15345;

            Mock.Arrange(() => _identityManagementServiceMock.GetNextLongIds(Arg.IsAny<string>(), 1))
                .Returns(new[] { newEventSetId }.ToList())
                .OccursOnce();

            ProcessEvents(events);

            VerifyEventSetCreated(newEventSetId, events.Last(), EventLevel.Critical);
            VerifyEventSetUpdated(events.First(), 1);
            _storageProcessorEventSkippedOutput.Should().BeEmpty();
            _storageProcessorEventFailedOutput.Should().BeEmpty();

            AssertMocks();
        }

        private void ProcessEvents(EventDetails[] inputEvents)
        {
            inputEvents.ToObservable()
                .Subscribe(_storageProcessor.Input);
            _storageProcessor.CompletionTask.Wait();
        }

        private void AssertMocks()
        {
            Mock.Assert(_processTypeManagerMock);
            Mock.Assert(_identityManagementServiceMock);
            Mock.Assert(_repositoryMock);
            Mock.Assert(_configurationMock);
        }

        private long GetCriticalEventsetTypeCode()
        {
            return EventSetType.CreateFromEventAndLevel(_criticalEvent, EventLevel.Critical).GetCode();
        }

        private void VerifyEventSetCreated(long eventSetId, EventDetails sourceEvent, EventLevel level)
        {
            _storageProcessorEventSetCreatedOutput
                .Should().HaveCount(1, "Expected that event set should be created while actually not.");
            VerifyCreatedEventSet(_storageProcessorEventSetCreatedOutput.First().EventSet, eventSetId, sourceEvent, level);
        }

        private void VerifyEventSetsCreatedForEachEvent(IList<long> eventSetIds, IList<EventDetails> sourceEvents, EventLevel level)
        {
            _storageProcessorEventSetCreatedOutput
                .Should()
                .HaveCount(sourceEvents.Count, "Expected that {0} event set should be created while actually {1}.", sourceEvents.Count, _storageProcessorEventSetCreatedOutput.Count);
            _storageProcessorEventSetCreatedOutput
                .Zip(sourceEvents, (eventSetWithEvents, sourceEvent) =>
                    new { EventSetWithEvents = eventSetWithEvents, SourceEvent = sourceEvent })
                .Zip(eventSetIds, (item, eventSetId) =>
                    new { EventSetId = eventSetId, EventSetWithEvents = item.EventSetWithEvents, SourceEvent = item.SourceEvent })
                .ToList()
                .ForEach(item => VerifyCreatedEventSet(item.EventSetWithEvents.EventSet, item.EventSetId, item.SourceEvent, level));
        }

        private void VerifyCreatedEventSet(EventSet eventSet, long eventSetId, EventDetails sourceEvent, EventLevel level)
        {
            eventSet.Id.Should().Be(eventSetId);
            eventSet.Level.Should().Be((byte)level);
            eventSet.EventTypeId.Should().Be(sourceEvent.EventTypeId);
            eventSet.ResourceCategory.Should().Be(sourceEvent.ResourceCategory);
            eventSet.ResourceId.Should().Be(sourceEvent.ResourceId);
            eventSet.SiteId.Should().Be(sourceEvent.SiteId);
            eventSet.EventsCount.Should().Be(1);
            eventSet.LastReadTime.Should().Be(sourceEvent.ReadTime);
            eventSet.LastUpdateTime.Should().Be(_currentTime);
            eventSet.CreationTime.Should().Be(_currentTime);
        }

        private void VerifyEventSetUpdated(EventDetails sourceEvent, int expectedEventCount)
        {
            _storageProcessorEventSetUpdatedOutput.Should().HaveCount(1, "Expected that event set should be updated while actually not.");
            VerifyUpdatedEventSet(_storageProcessorEventSetUpdatedOutput.First().EventSet, sourceEvent, expectedEventCount);
        }

        private void VerifyUpdatedEventSet(EventSet eventSet, EventDetails sourceEvent, int expectedEventCount)
        {
            eventSet.EventsCount.Should().Be(expectedEventCount);
            eventSet.LastReadTime.Should().Be(sourceEvent.ReadTime);
            eventSet.LastUpdateTime.Should().Be(_currentTime);
        }
    }
}