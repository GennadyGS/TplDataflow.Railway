﻿using AsyncProcessing.Core;
using EventProcessing.BusinessObjects;
using EventProcessing.Implementation;
using EventProcessing.Interfaces;
using FluentAssertions;
using LanguageExt;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace EventProcessing.Tests
{
    public class EventSetProcessorTest
    {
        private readonly Mock<IEventSetNotificationService> notificationServiceMock
            = new Mock<IEventSetNotificationService>();
        private readonly Mock<Func<IEnumerable<EventDetails>, IEnumerable<EventSetStorageProcessor.Result>>> storageProcessorDataflowMock 
            = new Mock<Func<IEnumerable<EventDetails>, IEnumerable<EventSetStorageProcessor.Result>>>();

        private readonly IAsyncProcessor<EventDetails, Tuple<bool, EventDetails>> processor;

        public EventSetProcessorTest()
        {
            var storageProcessor = AsyncProcessor.Create(storageProcessorDataflowMock.Object);
            processor = new EventSetProcessor(storageProcessor, notificationServiceMock.Object);
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

            storageProcessorDataflowMock
                .Setup(f =>
                    f(It.Is<IEnumerable<EventDetails>>(value => value.SequenceEqual(events))))
                .Returns(expectedStorageProcessorResult);
            notificationServiceMock.Setup(obj =>
                obj.NotifyEventSetCreated(
                    It.Is<EventSetAppearingNotification>(x => x.Id == eventSet.Id)))
                .Verifiable();

            var result = processor.InvokeSync(events);

            result.ShouldAllBeEquivalentTo(GetExpectedResult(true, events));

            notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetUpdated(It.IsAny<EventSetUpdateNotification>()),
                Times.Never);
            notificationServiceMock.Verify(obj =>
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

            storageProcessorDataflowMock
                .Setup(f =>
                    f(It.Is<IEnumerable<EventDetails>>(value => value.SequenceEqual(events))))
                .Returns(expectedStorageProcessorResult);
            notificationServiceMock.Setup(obj =>
                obj.NotifyEventSetUpdated(It.Is<EventSetUpdateNotification>(x =>
                    x.Id == eventSet.Id)))
                .Verifiable();
            notificationServiceMock.Setup(obj =>
                obj.NotifyEventArrived(It.Is<EventArrivedNotification>(x =>
                    x.EventSetId == eventSet.Id && x.Id == events.First().Id)))
                .Verifiable();

            var result = processor.InvokeSync(events);

            result.ShouldAllBeEquivalentTo(GetExpectedResult(true, events));

            notificationServiceMock.Verify(obj =>
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

            storageProcessorDataflowMock
                .Setup(f =>
                    f(It.Is<IEnumerable<EventDetails>>(value => value.SequenceEqual(events))))
                .Returns(expectedStorageProcessorResult);
            notificationServiceMock.Setup(obj =>
                obj.NotifyEventArrived(It.Is<EventArrivedNotification>(x =>
                    x.Id == events.First().Id && x.EventSetId == default(long))))
                .Verifiable();

            var result = processor.InvokeSync(events);

            result.ShouldAllBeEquivalentTo(GetExpectedResult(true, events));

            notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetCreated(It.IsAny<EventSetAppearingNotification>()),
                Times.Never);
            notificationServiceMock.Verify(obj =>
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

            storageProcessorDataflowMock
                .Setup(f =>
                    f(It.Is<IEnumerable<EventDetails>>(value => value.SequenceEqual(events))))
                .Returns(expectedStorageProcessorResult);

            var result = processor.InvokeSync(events);

            result.ShouldAllBeEquivalentTo(GetExpectedResult(false, events));

            notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetCreated(It.IsAny<EventSetAppearingNotification>()),
                Times.Never);
            notificationServiceMock.Verify(obj =>
                obj.NotifyEventSetUpdated(It.IsAny<EventSetUpdateNotification>()),
                Times.Never);
            notificationServiceMock.Verify(obj =>
                obj.NotifyEventArrived(It.IsAny<EventArrivedNotification>()),
                Times.Never);

            VerifyMocks();
        }

        private void VerifyMocks()
        {
            storageProcessorDataflowMock.Verify();
            notificationServiceMock.Verify();
        }

        private static IEnumerable<Tuple<bool, EventDetails>> GetExpectedResult(bool success, IList<EventDetails> events)
        {
            return events.Select(@event => Prelude.Tuple(success, @event));
        }
    }
}