using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using EventProcessing.BusinessObjects;
using EventProcessing.Interfaces;
using LanguageExt;
using TplDataFlow.Extensions.AsyncProcessing.Core;

namespace EventProcessing.Implementation
{
    /// <summary>
    ///     Processes <see cref="EventSet" /> based on new upcoming events.
    /// </summary>
    internal sealed class EventSetProcessor : IAsyncProcessor<EventDetails, Tuple<bool, EventDetails>>
    {
        private readonly IEventSetNotificationService _notificationService;
        private readonly IObservable<Tuple<bool, EventDetails>> _output;
        private readonly IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result> _storageProcessor;

        /// <summary> Initializes a new instance of the <see cref="EventSetProcessor" /> class. </summary>
        /// <param name="notificationService"> The notification service. </param>
        /// <param name="storageProcessor"> The storage processor </param>
        public EventSetProcessor(
            IAsyncProcessor<EventDetails, EventSetStorageProcessor.Result> storageProcessor,
            IEventSetNotificationService notificationService)
        {
            _storageProcessor = storageProcessor;
            _notificationService = notificationService;

            _output = _storageProcessor.SelectMany(SendNotifications);
        }

        void IObserver<EventDetails>.OnNext(EventDetails value)
        {
            _storageProcessor.OnNext(value);
        }

        void IObserver<EventDetails>.OnError(Exception error)
        {
            _storageProcessor.OnError(error);
        }

        void IObserver<EventDetails>.OnCompleted()
        {
            _storageProcessor.OnCompleted();
        }

        IDisposable IObservable<Tuple<bool, EventDetails>>.Subscribe(IObserver<Tuple<bool, EventDetails>> observer)
        {
            return _output.Subscribe(observer);
        }

        private IEnumerable<Tuple<bool, EventDetails>> SendNotifications(EventSetStorageProcessor.Result result)
        {
            return result.Match(
                transformEventSetCreated: HandleEventSetCreated, 
                transformEventSetUpdated: HandleEventSetUpdated, 
                transformEventSkipped: HandleEventSkipped, 
                transformEventFailed: HandleEventFailed);
        }

        private IEnumerable<Tuple<bool, EventDetails>> HandleEventSetCreated(EventSetWithEvents eventSetWithEvents)
        {
            NotifyEventSetCreated(eventSetWithEvents.EventSet);
            return eventSetWithEvents.Events.Select(@event => Prelude.Tuple(true, @event));
        }

        private IEnumerable<Tuple<bool, EventDetails>> HandleEventSetUpdated(EventSetWithEvents eventSetWithEvents)
        {
            NotifyEventSetUpdated(eventSetWithEvents.EventSet, eventSetWithEvents.Events);
            return eventSetWithEvents.Events.Select(@event => Prelude.Tuple(true, @event));
        }

        private IEnumerable<Tuple<bool, EventDetails>> HandleEventSkipped(EventDetails @event)
        {
            NotifyEventArrived(default(long), @event);
            return Prelude.List(Prelude.Tuple(true, @event));
        }

        private IEnumerable<Tuple<bool, EventDetails>> HandleEventFailed(EventDetails @event)
        {
            return Prelude.List(Prelude.Tuple(false, @event));
        }

        private void NotifyEventSetCreated(EventSet eventSet)
        {
            var notification = new EventSetAppearingNotification
            {
                Id = eventSet.Id,
                EventTypeId = eventSet.EventTypeId,
                ResourceId = eventSet.ResourceId
            };

            _notificationService.NotifyEventSetCreated(notification);
        }

        private void NotifyEventArrived(long eventSetId, EventDetails @event)
        {
            var notification = new EventArrivedNotification
            {
                Id = @event.Id,
                EventTypeId = @event.EventTypeId,
                EventSetId = eventSetId,
                AssociatedParameterValue = @event.AssociatedParameterValue,
                ResourceId = @event.ResourceId,
                GeographicRegionId = @event.GeographicRegionId,
                ReadTime = @event.ReadTime,
                Value = @event.Value,
                X = @event.X ?? 0,
                Y = @event.Y ?? 0,
                Z = @event.Z ?? 0,
                Category = @event.Category,
                AssociatedParameterTypeId = @event.AssociatedParameterTypeId,
                AssociatedParameterTypeName = @event.AssociatedParameterTypeName,
                StringValue = @event.StringValue,
                AssociatedParameterStringValue = @event.AssociatedParameterStringValue
            };
            _notificationService.NotifyEventArrived(notification);
        }

        private void NotifyEventSetUpdated(EventSet eventSet, IList<EventDetails> events)
        {
            var lastEvent = events.Last();
            var notification = new EventSetUpdateNotification
            {
                Id = eventSet.Id,
                ResourceId = eventSet.ResourceId,
                Level = eventSet.Level,
                Comment = eventSet.Comment,
                CompletedNote = eventSet.CompletedNote,
                CountDelta = events.Count,
                EventLastReadTime = eventSet.LastReadTime,
                Owner = eventSet.Owner,
                Status = eventSet.Status,
                LastReadTime = eventSet.LastUpdateTime,
                AcceptedBy = eventSet.AcceptedBy,
                AcceptedTime = eventSet.AcceptedTime,
                Value = lastEvent.Value,
                CompletedTime = eventSet.CompletedTime,
                AssociatedParameterValue = lastEvent.AssociatedParameterValue,
                FailureMode = eventSet.FailureMode
            };
            _notificationService.NotifyEventSetUpdated(notification);
            events.ToList().ForEach(@event => NotifyEventArrived(eventSet.Id, @event));
        }
    }
}