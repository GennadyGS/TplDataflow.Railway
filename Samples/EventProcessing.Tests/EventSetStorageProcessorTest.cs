﻿using System;
using AsyncProcessing.Core;
using EventProcessing.BusinessObjects;
using EventProcessing.Implementation;
using EventProcessing.Interfaces;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EventProcessing.Tests
{
    public abstract class EventSetStorageProcessorTest
    {
        private const int EventBatchSize = 1000;
        private const string EventBatchTimeout = "00:00:05";

        private const int EventGroupBatchSize = 25;
        private const string EventGroupBatchTimeout = "00:00:05";

        private readonly EventDetails _criticalEvent;
        private readonly EventDetails _informationalEvent;
        private readonly DateTime _currentTime = DateTime.UtcNow;

        private readonly Mock<IIdentityManagementService> _identityManagementServiceMock 
            = new Mock<IIdentityManagementService>();
        private readonly Mock<IEventSetProcessTypeManager> _processTypeManagerMock 
            = new Mock<IEventSetProcessTypeManager>();
        private readonly Mock<IEventSetRepository> _repositoryMock 
            = new Mock<IEventSetRepository>();
        private readonly Mock<IEventSetConfiguration> _configurationMock 
            = new Mock<IEventSetConfiguration>();

        private readonly IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result> _storageProcessor;

        protected EventSetStorageProcessorTest(EventSetStorageProcessor.IFactory storageProcessorFactory)
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

            SetupConfigurationMock();

            _storageProcessor = storageProcessorFactory.CreateStorageProcessor(
                () => _repositoryMock.Object,
                _identityManagementServiceMock.Object,
                _processTypeManagerMock.Object,
                _configurationMock.Object,
                () => _currentTime);
        }

        [Fact]
        public void WhenProcessEmptyList_ResultShouldBeEmpty()
        {
            var result = _storageProcessor.InvokeSync(new EventDetails[] {});

            result.Should().BeEmpty();

            _processTypeManagerMock.Verify(obj => 
                obj.GetProcessTypeAsync(It.IsAny<int>(), It.IsAny<EventTypeCategory>()),
                Times.Never);
            _identityManagementServiceMock.Verify(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), It.IsAny<int>()),
                Times.Never);
            _repositoryMock.Verify(obj => 
                obj.ApplyChangesAsync(It.IsAny<IList<EventSet>>(), It.IsAny<IList<EventSet>>()),
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenEventSetProcessTypeWasNotFound_EventShouldBeFailed()
        {
            _processTypeManagerMock.Setup(obj => 
                obj.GetProcessTypeAsync(It.IsAny<int>(), It.IsAny<EventTypeCategory>()))
                .Returns(Task.FromResult((EventSetProcessType) null))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] {_informationalEvent});

            result.Should().HaveCount(1);
            result.First().VerifyEventFailed(_informationalEvent);

            _identityManagementServiceMock.Verify(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), It.IsAny<int>()),
                Times.Never);
            _repositoryMock.Verify(obj => 
                obj.ApplyChangesAsync(It.IsAny<IList<EventSet>>(), It.IsAny<IList<EventSet>>()),
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenEventSetProcessTypeThrowsException_EventShouldBeFailed()
        {
            _processTypeManagerMock.Setup(obj => 
                obj.GetProcessTypeAsync(It.IsAny<int>(), It.IsAny<EventTypeCategory>()))
                .Throws<Exception>()
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] {_informationalEvent});

            result.Should().HaveCount(1);
            result.First().VerifyEventFailed(_informationalEvent);

            VerifyMocks();
        }

        [Fact]
        public void WhenFindLastEventSetsThrowsException_EventShouldBeFailed()
        {
            var processType = new EventSetProcessType {Level = (byte) EventLevel.Critical};
            _processTypeManagerMock.Setup(obj => 
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => 
                        typeCodes.SequenceEqual(new[] {GetCriticalEventsetTypeCode()}))))
                .Throws<Exception>()
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] {_criticalEvent});

            result.Should().HaveCount(1);
            result.First().VerifyEventFailed(_criticalEvent);

            _identityManagementServiceMock.Verify(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), It.IsAny<int>()), 
                Times.Never);
            _repositoryMock.Verify(obj => 
                obj.ApplyChangesAsync(It.IsAny<IList<EventSet>>(), It.IsAny<IList<EventSet>>()), 
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenGetNextLongIdsThrowsException_EventShouldBeFailed()
        {
            var processType = new EventSetProcessType {Level = (byte) EventLevel.Critical};
            _processTypeManagerMock.Setup(obj => 
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => 
                        typeCodes.SequenceEqual(new[] {GetCriticalEventsetTypeCode()}))))
                .Returns(Task.FromResult<IList<EventSet>>(new EventSet[] {}))
                .Verifiable();
            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Throws<Exception>()
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] {_criticalEvent});

            result.Should().HaveCount(1);
            result.First().VerifyEventFailed(_criticalEvent);

            _repositoryMock.Verify(obj => 
                obj.ApplyChangesAsync(It.IsAny<IList<EventSet>>(), It.IsAny<IList<EventSet>>()), 
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenApplyChangesThrowsException_EventShouldBeFailed()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Critical };
            _processTypeManagerMock.Setup(obj => 
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i ==_criticalEvent.EventTypeId), 
                    It.Is<EventTypeCategory>(i => i ==_criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new EventSet[] {} ))
                .Verifiable();

            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(It.IsAny<IList<EventSet>>(), It.IsAny<IList<EventSet>>()))
                .Throws<Exception>()
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }.ToList()))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventFailed(_criticalEvent);

            VerifyMocks();
        }

        [Fact]
        public void WhenInformationalEventOccured_EventShouldBeSkipped()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Information };

            _processTypeManagerMock.Setup(obj => 
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _informationalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i ==_informationalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _informationalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSkipped(_informationalEvent);

            _identityManagementServiceMock.Verify(obj =>
                obj.GetNextLongIdsAsync(It.IsAny<string>(), It.IsAny<int>()),
                Times.Never);
            _repositoryMock.Verify(obj =>
                obj.ApplyChangesAsync(It.IsAny<IList<EventSet>>(), It.IsAny<IList<EventSet>>()),
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenEventOccurred_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Critical };
            _processTypeManagerMock.Setup(obj => 
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i ==_criticalEvent.EventTypeId), 
                    It.Is<EventTypeCategory>(i => i== _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new EventSet[] { }))
                .Verifiable();

            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.Is<IList<EventSet>>(sets => sets.Count == 1),
                    It.Is<IList<EventSet>>(sets => sets.Count == 0)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

            VerifyMocks();
        }

        [Fact]
        public void WhenEventSetWasFoundInDb_ItShouldBeUpdated()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Critical, Threshold = TimeSpan.FromHours(1) };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                Level = (byte)EventLevel.Critical,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => 
                        typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();

            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.Is<IList<EventSet>>(sets => sets.Count == 0),
                    It.Is<IList<EventSet>>(sets => sets.Count == 1)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetUpdated(_criticalEvent, 1, _currentTime);

            _identityManagementServiceMock.Verify(obj =>
                obj.GetNextLongIdsAsync(It.IsAny<string>(), It.IsAny<int>()),
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenEventSetIsOutdated_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType { Level = (byte)EventLevel.Critical };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            var eventSet = new EventSet
            {
                Status = (byte)EventSetStatus.Completed,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.Is<IList<EventSet>>(sets => sets.Count == 1),
                    It.Is<IList<EventSet>>(sets => sets.Count == 0)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

            VerifyMocks();
        }

        [Fact]
        public void WhenThresholdOccurred_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10)
            };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-11),
                TypeCode = GetCriticalEventsetTypeCode()
            };

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.Is<IList<EventSet>>(sets => sets.Count == 1),
                    It.Is<IList<EventSet>>(sets => sets.Count == 0)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

            VerifyMocks();
        }

        [Fact]
        public void WhenCurrentEventSetCompleted_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10)
            };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                Status = (byte)EventSetStatus.Completed,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.Is<IList<EventSet>>(sets => sets.Count == 1),
                    It.Is<IList<EventSet>>(sets => sets.Count == 0)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

            VerifyMocks();
        }

        [Fact]
        public void WhenCurrentAutocompleteTimeOccurredAndStatusIsNew_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10),
                AutoComplete = true,
                AutoCompleteTimeout = TimeSpan.FromSeconds(9)
            };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                CreationTime = _currentTime.AddMinutes(-1),
                Status = (byte)EventSetStatus.New,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => 
                        typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.Is<IList<EventSet>>(sets => sets.Count == 1),
                    It.Is<IList<EventSet>>(sets => sets.Count == 0)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

            VerifyMocks();
        }

        [Fact]
        public void WhenCurrentAutocompleteTimeOccurredAndStatusIsAccepted_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10),
                AutoComplete = true,
                AutoCompleteTimeout = TimeSpan.FromSeconds(9)
            };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                AcceptedTime = _currentTime.AddMinutes(-1),
                Status = (byte)EventSetStatus.Accepted,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => 
                        typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.Is<IList<EventSet>>(sets => sets.Count == 1),
                    It.Is<IList<EventSet>>(sets => sets.Count == 0)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

            VerifyMocks();
        }

        [Fact]
        public void WhenCurrentAutocompleteTimeOccurredAndStatusIsRejected_NewEventSetShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(10),
                AutoComplete = true,
                AutoCompleteTimeout = TimeSpan.FromSeconds(9)
            };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

            var eventSet = new EventSet
            {
                LastReadTime = _currentTime.AddMinutes(-1),
                AcceptedTime = _currentTime.AddMinutes(-1),
                Status = (byte)EventSetStatus.Rejected,
                TypeCode = GetCriticalEventsetTypeCode()
            };

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => 
                        typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.Is<IList<EventSet>>(sets => sets.Count == 1),
                    It.Is<IList<EventSet>>(sets => sets.Count == 0)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(new[] { _criticalEvent });

            result.Should().HaveCount(1);
            result.First().VerifyEventSetCreated(newEventSetId, _criticalEvent, EventLevel.Critical, _currentTime);

            VerifyMocks();
        }

        [Fact]
        public void WhenThresholdOccurredBetweenEvents_NewEventSetsForEachEventShouldBeCreated()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(5)
            };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

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

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => 
                        typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();
            _repositoryMock.Setup(obj =>
                obj.ApplyChangesAsync(
                    It.IsAny<IList<EventSet>>(),
                    It.IsAny<IList<EventSet>>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // TODO: Refactoring
            var eventSetIds = new long[] { 15345, 15346 };
            int currentEventSetIdIndex = 0;
            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(
                    It.IsAny<string>(), 
                    It.Is<int>(i => i > 0 && i <= eventSetIds.Length)))
                .Returns((string sequenceName, int amount) =>
                {
                    var res = eventSetIds
                        .Skip(Interlocked.Add(ref currentEventSetIdIndex, amount) - amount)
                        .Take(amount)
                        .ToList();
                    return Task.FromResult<IList<long>>(res);
                });

            var result = _storageProcessor.InvokeSync(events);

            result.VerifyEventSetsCreatedForEachEvent(
                eventSetIds, events, EventLevel.Critical, _currentTime);

            VerifyMocks();
        }

        [Fact]
        public void WhenThresholdOccurredBeforeSecondEvent_EventSetShouldBeUpdatedAndNewEventSetsShouldBeCreatedForSecondEvent()
        {
            var processType = new EventSetProcessType
            {
                Level = (byte)EventLevel.Critical,
                Threshold = TimeSpan.FromMinutes(5)
            };
            _processTypeManagerMock.Setup(obj =>
                obj.GetProcessTypeAsync(
                    It.Is<int>(i => i == _criticalEvent.EventTypeId),
                    It.Is<EventTypeCategory>(i => i == _criticalEvent.Category)))
                .Returns(Task.FromResult(processType))
                .Verifiable();

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

            _repositoryMock.Setup(obj => 
                obj.FindLastEventSetsByTypeCodesAsync(
                    It.Is<IList<long>>(typeCodes => 
                        typeCodes.SequenceEqual(new[] { GetCriticalEventsetTypeCode() }))))
                .Returns(Task.FromResult<IList<EventSet>>(new[] { eventSet }))
                .Verifiable();
            _repositoryMock.Setup(obj => 
                obj.ApplyChangesAsync(
                    It.IsAny<IList<EventSet>>(),
                    It.IsAny<IList<EventSet>>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            long newEventSetId = 15345;

            _identityManagementServiceMock.Setup(obj => 
                obj.GetNextLongIdsAsync(It.IsAny<string>(), 1))
                .Returns(Task.FromResult<IList<long>>(new[] { newEventSetId }))
                .Verifiable();

            var result = _storageProcessor.InvokeSync(events);

            result.Should().HaveCount(2);
            result[0].VerifyEventSetCreated(newEventSetId, events.Last(), EventLevel.Critical, _currentTime);
            result[1].VerifyEventSetUpdated(events.First(), 1, _currentTime);

            VerifyMocks();
        }

        private long GetCriticalEventsetTypeCode()
        {
            return EventSetType.CreateFromEventAndLevel(_criticalEvent, EventLevel.Critical).GetCode();
        }

        private void SetupConfigurationMock()
        {
            _configurationMock
                .Setup(configuration => configuration.EventBatchSize)
                .Returns(EventBatchSize);
            _configurationMock
                .Setup(configuration => configuration.EventBatchTimeout)
                .Returns(TimeSpan.Parse(EventBatchTimeout));
            _configurationMock
                .Setup(configuration => configuration.EventGroupBatchSize)
                .Returns(EventGroupBatchSize);
            _configurationMock
                .Setup(configuration => configuration.EventGroupBatchTimeout)
                .Returns(TimeSpan.Parse(EventGroupBatchTimeout));
        }

        private void VerifyMocks()
        {
            _processTypeManagerMock.Verify();
            _identityManagementServiceMock.Verify();
            _repositoryMock.Verify();
            _configurationMock.Verify();
        }

        public class EnumerableBatchSyncImpl : EventSetStorageProcessorTest
        {
            public EnumerableBatchSyncImpl() : base(new EventSetStorageProcessor.EnumerableBatchSyncFactory())
            {
            }
        }

        public class EnumerableBatchAsyncImpl : EventSetStorageProcessorTest
        {
            public EnumerableBatchAsyncImpl() : base(new EventSetStorageProcessor.EnumerableBatchAsyncFactory())
            {
            }
        }

        public class ObservableBatchSyncImpl : EventSetStorageProcessorTest
        {
            public ObservableBatchSyncImpl() : base(new EventSetStorageProcessor.ObservableBatchSyncFactory())
            {
            }
        }

        public class ObservableBatchAsyncImpl : EventSetStorageProcessorTest
        {
            public ObservableBatchAsyncImpl() : base(new EventSetStorageProcessor.ObservableBatchAsyncFactory())
            {
            }
        }

        public class TplDataflowBatchSyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowBatchSyncImpl() : base(new EventSetStorageProcessor.TplDataflowBatchSyncFactory())
            {
            }
        }

        public class TplDataflowBatchAsyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowBatchAsyncImpl() : base(new EventSetStorageProcessor.TplDataflowBatchAsyncFactory())
            {
            }
        }

        public class DataflowBatchSyncImpl : EventSetStorageProcessorTest
        {
            public DataflowBatchSyncImpl() : base(new EventSetStorageProcessor.DataflowBatchSyncFactory())
            {
            }
        }

        public class DataflowBatchAsyncImpl : EventSetStorageProcessorTest
        {
            public DataflowBatchAsyncImpl() : base(new EventSetStorageProcessor.DataflowBatchAsyncFactory())
            {
            }
        }

        public class TplDataflowDataflowBatchSyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowDataflowBatchSyncImpl() : base(new EventSetStorageProcessor.TplDataflowDataflowBatchSyncFactory())
            {
            }
        }

        public class TplDataflowDataflowBatchAsyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowDataflowBatchAsyncImpl() : base(new EventSetStorageProcessor.TplDataflowDataflowBatchAsyncFactory())
            {
            }
        }

        public class EnumerableIndividualSyncImpl : EventSetStorageProcessorTest
        {
            public EnumerableIndividualSyncImpl() : base(new EventSetStorageProcessor.EnumerableIndividualSyncFactory())
            {
            }
        }

        public class EnumerableIndividualAsyncImpl : EventSetStorageProcessorTest
        {
            public EnumerableIndividualAsyncImpl() : base(new EventSetStorageProcessor.EnumerableIndividualAsyncFactory())
            {
            }
        }

        public class ObservableIndividualSyncImpl : EventSetStorageProcessorTest
        {
            public ObservableIndividualSyncImpl() : base(new EventSetStorageProcessor.ObservableIndividualSyncFactory())
            {
            }
        }

        public class ObservableIndividualAsyncImpl : EventSetStorageProcessorTest
        {
            public ObservableIndividualAsyncImpl() : base(new EventSetStorageProcessor.ObservableIndividualAsyncFactory())
            {
            }
        }

        public class TplDataflowIndividualSyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowIndividualSyncImpl() : base(new EventSetStorageProcessor.TplDataflowIndividualSyncFactory())
            {
            }
        }

        public class TplDataflowIndividualAsyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowIndividualAsyncImpl() : base(new EventSetStorageProcessor.TplDataflowIndividualAsyncFactory())
            {
            }
        }

        public class DataflowIndividualSyncImpl : EventSetStorageProcessorTest
        {
            public DataflowIndividualSyncImpl() : base(new EventSetStorageProcessor.DataflowIndividualSyncFactory())
            {
            }
        }

        public class DataflowIndividualAsyncImpl : EventSetStorageProcessorTest
        {
            public DataflowIndividualAsyncImpl() : base(new EventSetStorageProcessor.DataflowIndividualAsyncFactory())
            {
            }
        }

        public class TplDataflowDataflowIndividualSyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowDataflowIndividualSyncImpl() : base(new EventSetStorageProcessor.TplDataflowDataflowIndividualSyncFactory())
            {
            }
        }

        public class TplDataflowDataflowIndividualAsyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowDataflowIndividualAsyncImpl() : base(new EventSetStorageProcessor.TplDataflowDataflowIndividualAsyncFactory())
            {
            }
        }
    }
}