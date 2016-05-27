using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using TplDataflow.Extensions.Example.BusinessObjects;
using TplDataflow.Extensions.Example.Implementation;
using TplDataflow.Extensions.Example.Interfaces;
using TplDataFlow.Extensions.AsyncProcessing.Core;
using Xunit;
using static LanguageExt.Prelude;

namespace TplDataflow.Extensions.Example.Test
{
    public class EventSetProcessorTest
    {
        private readonly Mock<IEventSetNotificationService> _notificationServiceMock 
            = new Mock<IEventSetNotificationService>();
        private readonly Mock<Func<IEnumerable<EventDetails>, IEnumerable<EventSetStorageProcessor.Result>>> _storageProcessorDataflowMock 
            = new Mock<Func<IEnumerable<EventDetails>, IEnumerable<EventSetStorageProcessor.Result>>>();

        private readonly IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result> _storageProcessor;

        private readonly IAsyncProcessor<EventDetails, Tuple<bool, EventDetails>> _processor;

        public EventSetProcessorTest()
        {
            _storageProcessor = new AsyncProcessor<EventDetails, EventSetStorageProcessor.Result>(_storageProcessorDataflowMock.Object);

            _processor = new EventSetProcessor(_storageProcessor, _notificationServiceMock.Object);
        }

        [Fact]
        public void WhenEventSetCreated_NotificationsShouldBeSent()
        {
            var events = new[] { new EventDetails { Id = 3423, ReadTime = DateTime.UtcNow } };
            var eventSet = new EventSet { Id = 5465 };

            var expectedStorageProcessorResult = List(EventSetStorageProcessor.Result.CreateEventSetCreated(
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
            var events = new[] { new EventDetails { Id = 3423, ReadTime = DateTime.UtcNow } };
            var eventSet = new EventSet { Id = 5465 };

            var expectedStorageProcessorResult = List(EventSetStorageProcessor.Result.CreateEventSetUpdated(
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
            var events = new[] { new EventDetails { Id = 3423, ReadTime = DateTime.UtcNow } };

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
            var events = new[] { new EventDetails { Id = 3423, ReadTime = DateTime.UtcNow } };

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
            return events.Select(@event => Tuple(success, @event));
        }
    }
}