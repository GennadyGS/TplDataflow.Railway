using TplDataflow.Extensions.Example.BusinessObjects;

namespace TplDataflow.Extensions.Example.Interfaces
{
    /// <summary>
    /// Notifies client about changes of event sets.
    /// </summary>
    public interface IEventSetNotificationService
    {
        /// <summary>
        /// Notifies the event set created.
        /// </summary>
        /// <param name="notification">The notification.</param>
        void NotifyEventSetCreated(EventSetAppearingNotification notification);

        /// <summary>
        /// Notifies the event set updated.
        /// </summary>
        /// <param name="notification">The notification.</param>
        void NotifyEventSetUpdated(EventSetUpdateNotification notification);

        /// <summary>
        /// Notifies the event arrived.
        /// </summary>
        /// <param name="eventDetails">The event details.</param>
        void NotifyEventArrived(EventArrivedNotification eventDetails);
    }
}