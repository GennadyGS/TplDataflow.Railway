using System;
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
                () => new EventSetRepositoryAsyncProxy(_repositoryMock.Object),
                new IdentityManagementServiceAsyncProxy(_identityManagementServiceMock.Object),
                new ProcessTypManagerAsyncProxy(_processTypeManagerMock.Object),
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

        public class EnumerableImpl : EventSetStorageProcessorTest
        {
            public EnumerableImpl() : base(new EventSetStorageProcessor.EnumerableFactory())
            {
            }
        }

        public class EnumerableAsyncImpl : EventSetStorageProcessorTest
        {
            public EnumerableAsyncImpl() : base(new EventSetStorageProcessor.EnumerableAsyncFactory())
            {
            }
        }

        public class EnumerableOneByOneImpl : EventSetStorageProcessorTest
        {
            public EnumerableOneByOneImpl() : base(new EventSetStorageProcessor.EnumerableOneByOneFactory())
            {
            }
        }

        public class EnumerableOneByOneAsyncImpl : EventSetStorageProcessorTest
        {
            public EnumerableOneByOneAsyncImpl() : base(new EventSetStorageProcessor.EnumerableOneByOneAsyncFactory())
            {
            }
        }

        public class ObservableImpl : EventSetStorageProcessorTest
        {
            public ObservableImpl() : base(new EventSetStorageProcessor.ObservableFactory())
            {
            }
        }

        public class ObservableAsyncImpl : EventSetStorageProcessorTest
        {
            public ObservableAsyncImpl() : base(new EventSetStorageProcessor.ObservableAsyncFactory())
            {
            }
        }

        public class ObservableOneByOneImpl : EventSetStorageProcessorTest
        {
            public ObservableOneByOneImpl() : base(new EventSetStorageProcessor.ObservableOneByOneFactory())
            {
            }
        }

        public class ObservableOneByOneAsyncImpl : EventSetStorageProcessorTest
        {
            public ObservableOneByOneAsyncImpl() : base(new EventSetStorageProcessor.ObservableOneByOneAsyncFactory())
            {
            }
        }

        public class TplDataflowImpl : EventSetStorageProcessorTest
        {
            public TplDataflowImpl() : base(new EventSetStorageProcessor.TplDataflowFactory())
            {
            }
        }

        public class TplDataflowAsyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowAsyncImpl() : base(new EventSetStorageProcessor.TplDataflowAsyncFactory())
            {
            }
        }

        public class TplDataflowOneByOneImpl : EventSetStorageProcessorTest
        {
            public TplDataflowOneByOneImpl() : base(new EventSetStorageProcessor.TplDataflowOneByOneFactory())
            {
            }
        }

        public class TplDataflowOneByOneAsyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowOneByOneAsyncImpl() : base(new EventSetStorageProcessor.TplDataflowOneByOneAsyncFactory())
            {
            }
        }

        public class DataflowImpl : EventSetStorageProcessorTest
        {
            public DataflowImpl() : base(new EventSetStorageProcessor.DataflowFactory())
            {
            }
        }

        public class DataflowAsyncImpl : EventSetStorageProcessorTest
        {
            public DataflowAsyncImpl() : base(new EventSetStorageProcessor.DataflowAsyncFactory())
            {
            }
        }

        public class DataflowOneByOneImpl : EventSetStorageProcessorTest
        {
            public DataflowOneByOneImpl() : base(new EventSetStorageProcessor.DataflowOneByOneFactory())
            {
            }
        }

        public class DataflowOneByOneAsyncImpl : EventSetStorageProcessorTest
        {
            public DataflowOneByOneAsyncImpl() : base(new EventSetStorageProcessor.DataflowOneByOneAsyncFactory())
            {
            }
        }

        public class TplDataflowDataflowImpl : EventSetStorageProcessorTest
        {
            public TplDataflowDataflowImpl() : base(new EventSetStorageProcessor.TplDataflowDataflowFactory())
            {
            }
        }

        public class TplDataflowDataflowAsyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowDataflowAsyncImpl() : base(new EventSetStorageProcessor.TplDataflowDataflowAsyncFactory())
            {
            }
        }

        public class TplDataflowDataflowOneByOneImpl : EventSetStorageProcessorTest
        {
            public TplDataflowDataflowOneByOneImpl() : base(new EventSetStorageProcessor.TplDataflowDataflowOneByOneFactory())
            {
            }
        }

        public class TplDataflowDataflowOneByOneAsyncImpl : EventSetStorageProcessorTest
        {
            public TplDataflowDataflowOneByOneAsyncImpl() : base(new EventSetStorageProcessor.TplDataflowDataflowOneByOneAsyncFactory())
            {
            }
        }
    }
}