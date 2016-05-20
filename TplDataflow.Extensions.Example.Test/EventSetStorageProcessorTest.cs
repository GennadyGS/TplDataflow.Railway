using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;
using TplDataflow.Extensions.Example.BusinessObjects;
using TplDataflow.Extensions.Example.Implementation;
using TplDataflow.Extensions.Example.Interfaces;
using TplDataFlow.Extensions.AsyncProcessing;

namespace TplDataflow.Extensions.Example.Test
{
    [TestClass]
    public class EventSetStorageProcessorTest
    {
        private const int EventBatchSize = 1000;
        private const string EventBatchTimeout = "00:00:05";

        private const int EventGroupBatchSize = 25;
        private const string EventGroupBatchTimeout = "00:00:05";

        private IEventSetRepository _repositoryMock;
        private IIdentityManagementService _identityManagementServiceMock;
        private IEventSetProcessTypeManager _processTypeManagerMock;
        private IEventSetConfiguration _configurationMock;

        private IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result> _storageProcessor;

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

            ArrangeMocks();

            _storageProcessor = new EventSetStorageProcessor.TplDataflowImpl(
                () => _repositoryMock,
                _identityManagementServiceMock,
                _processTypeManagerMock,
                _configurationMock,
                () => _currentTime);
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

            var result = _storageProcessor.InvokeSync(new[] { _informationalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSkipped(_informationalEvent);

            AssertMocks();
        }

        [TestMethod]
        public void WhenEventSetProcessTypeWasNotFound_EventShouldBeFailed()
        {
            Mock.Arrange(() => _processTypeManagerMock.GetProcessType(Arg.AnyInt, Arg.IsAny<EventTypeCategory>()))
                .Returns((EventSetProcessType)null);

            var result = _storageProcessor.InvokeSync(new[] { _informationalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventFailed(_informationalEvent);
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

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

            AssertMocks();
        }

        // TODO: Verify all exceptions

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

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetUpdated(_criticalEvent, 1, _currentTime);

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

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

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

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

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

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

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

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

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

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

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

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

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

            var result = _storageProcessor.InvokeSync(events);

            result.VerifyEventSetsCreatedForEachEvent(
                eventSetIds, events, EventLevel.Critical, _currentTime);

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

            var result = _storageProcessor.InvokeSync(events);

            result.Should().HaveCount(2);
            result[0].VerifyEventSetCreated(newEventSetId, events.Last(), EventLevel.Critical, _currentTime);
            result[1].VerifyEventSetUpdated(events.First(), 1, _currentTime);

            AssertMocks();
        }

        private long GetCriticalEventsetTypeCode()
        {
            return EventSetType.CreateFromEventAndLevel(_criticalEvent, EventLevel.Critical).GetCode();
        }

        private void ArrangeMocks()
        {
            _configurationMock
                .Arrange(configuration => configuration.EventBatchSize)
                .Returns(EventBatchSize);
            _configurationMock
                .Arrange(configuration => configuration.EventBatchTimeout)
                .Returns(TimeSpan.Parse(EventBatchTimeout));
            _configurationMock
                .Arrange(configuration => configuration.EventGroupBatchSize)
                .Returns(EventGroupBatchSize);
            _configurationMock
                .Arrange(configuration => configuration.EventGroupBatchTimeout)
                .Returns(TimeSpan.Parse(EventGroupBatchTimeout));
        }

        private void AssertMocks()
        {
            Mock.Assert(_processTypeManagerMock);
            Mock.Assert(_identityManagementServiceMock);
            Mock.Assert(_repositoryMock);
            Mock.Assert(_configurationMock);
        }
    }
}