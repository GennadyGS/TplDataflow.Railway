using System;
using System.Collections.Generic;
using System.Linq;
using AsyncProcessing.Core;
using EventProcessing.BusinessObjects;
using EventProcessing.Implementation;
using EventProcessing.Interfaces;
using FluentAssertions;
using LanguageExt;
using Moq;
using Xunit;

namespace EventProcessing.Tests
{
    public class EventSetProcessorTest
    {
        private readonly Mock<IEventSetNotificationService> _notificationServiceMock
            = new Mock<IEventSetNotificationService>();
        private readonly Mock<Func<IEnumerable<EventDetails>, IEnumerable<EventSetStorageProcessor.Result>>> _storageProcessorDataflowMock 
            = new Mock<Func<IEnumerable<EventDetails>, IEnumerable<EventSetStorageProcessor.Result>>>();

        private readonly IAsyncProcessor<EventDetails, Tuple<bool, EventDetails>> _processor;

        public EventSetProcessorTest()
        {
            var storageProcessor =
                new AsyncProcessor<EventDetails, EventSetStorageProcessor.Result>(_storageProcessorDataflowMock.Object);

            _processor = new EventSetProcessor(storageProcessor, _notificationServiceMock.Object);
        }

        [Fact]
        public void WhenEventSetCreated_NotificationsShouldBeSent()
        {
            var events = new[] {new EventDetails {Id = 3423, ReadTime = DateTime.UtcNow}};
            var eventSet = new EventSet {Id = 5465};

            var expectedStorageProcessorResult = Prelude.List(EventSetStorageProcessor.Result.CreateEventSetCreated(
                new EventSetWithEvents
                {
                    EventSet = eventSet,
                    Events = events
                }));

            _storageProcessorDataflowMock
                .Setup(f =>
                    f(It.Is<IEnumerable<EventDetails>>(value => value.SequenceEqual(events))))
                .Returns(expectedStorageProcessorResult);
            _notificationServiceMock.Setup(obj =>
                obj.NotifyEventSetCreated(
                    It.Is<EventSetAppearingNotification>(x => x.Id == eventSet.Id)))
                .Verifiable();

            var result = _processor.InvokeSync(events);

            result.ShouldAllBeEquivalentTo(GetExpectedResult(true, events));

            _notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetUpdated(It.IsAny<EventSetUpdateNotification>()),
                Times.Never);
            _notificationServiceMock.Verify(obj =>
                obj.NotifyEventArrived(
                    It.Is<EventArrivedNotification>(x =>
                        x.EventSetId == eventSet.Id && x.Id == events.First().Id)),
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenEventSetUpdated_NotificationsShouldBeSent()
        {
            var events = new[] {new EventDetails {Id = 3423, ReadTime = DateTime.UtcNow}};
            var eventSet = new EventSet {Id = 5465};

            var expectedStorageProcessorResult = Prelude.List(EventSetStorageProcessor.Result.CreateEventSetUpdated(
                new EventSetWithEvents
                {
                    EventSet = eventSet,
                    Events = events
                }));

            _storageProcessorDataflowMock
                .Setup(f =>
                    f(It.Is<IEnumerable<EventDetails>>(value => value.SequenceEqual(events))))
                .Returns(expectedStorageProcessorResult);
            _notificationServiceMock.Setup(obj =>
                obj.NotifyEventSetUpdated(It.Is<EventSetUpdateNotification>(x =>
                    x.Id == eventSet.Id)))
                .Verifiable();
            _notificationServiceMock.Setup(obj =>
                obj.NotifyEventArrived(It.Is<EventArrivedNotification>(x =>
                    x.EventSetId == eventSet.Id && x.Id == events.First().Id)))
                .Verifiable();

            var result = _processor.InvokeSync(events);

            result.ShouldAllBeEquivalentTo(GetExpectedResult(true, events));

            _notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetCreated(It.IsAny<EventSetAppearingNotification>()),
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenEventsSkipped_NotificationShouldBeSent()
        {
            var events = new[] {new EventDetails {Id = 3423, ReadTime = DateTime.UtcNow}};

            var expectedStorageProcessorResult = events.Select(
                EventSetStorageProcessor.Result.CreateEventSkipped);

            _storageProcessorDataflowMock
                .Setup(f =>
                    f(It.Is<IEnumerable<EventDetails>>(value => value.SequenceEqual(events))))
                .Returns(expectedStorageProcessorResult);
            _notificationServiceMock.Setup(obj =>
                obj.NotifyEventArrived(It.Is<EventArrivedNotification>(x =>
                    x.Id == events.First().Id && x.EventSetId == default(long))))
                .Verifiable();

            var result = _processor.InvokeSync(events);

            result.ShouldAllBeEquivalentTo(GetExpectedResult(true, events));

            _notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetCreated(It.IsAny<EventSetAppearingNotification>()),
                Times.Never);
            _notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetUpdated(It.IsAny<EventSetUpdateNotification>()),
                Times.Never);

            VerifyMocks();
        }

        [Fact]
        public void WhenEventProcessingFailed_NotificationShouldNotBeSent()
        {
            var events = new[] {new EventDetails {Id = 3423, ReadTime = DateTime.UtcNow}};

            var expectedStorageProcessorResult = events.Select(
                EventSetStorageProcessor.Result.CreateEventFailed);

            _storageProcessorDataflowMock
                .Setup(f =>
                    f(It.Is<IEnumerable<EventDetails>>(value => value.SequenceEqual(events))))
                .Returns(expectedStorageProcessorResult);

            var result = _processor.InvokeSync(events);

            result.ShouldAllBeEquivalentTo(GetExpectedResult(false, events));

            _notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetCreated(It.IsAny<EventSetAppearingNotification>()),
                Times.Never);
            _notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetUpdated(It.IsAny<EventSetUpdateNotification>()),
                Times.Never);
            _notificationServiceMock.Verify(obj =>
                obj.NotifyEventArrived(It.IsAny<EventArrivedNotification>()),
                Times.Never);

            VerifyMocks();
        }

        private void VerifyMocks()
        {
            _storageProcessorDataflowMock.Verify();
            _notificationServiceMock.Verify();
        }

        private static IEnumerable<Tuple<bool, EventDetails>> GetExpectedResult(bool success, IList<EventDetails> events)
        {
            return events.Select(@event => Prelude.Tuple(success, @event));
        }
    }
}