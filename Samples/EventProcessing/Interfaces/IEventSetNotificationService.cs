using EventProcessing.BusinessObjects;

namespace EventProcessing.Interfaces
{
    public interface IEventSetNotificationService
    {
        void NotifyEventSetCreated(EventSetAppearingNotification notification);

        void NotifyEventSetUpdated(EventSetUpdateNotification notification);

        void NotifyEventArrived(EventArrivedNotification eventDetails);
    }
}