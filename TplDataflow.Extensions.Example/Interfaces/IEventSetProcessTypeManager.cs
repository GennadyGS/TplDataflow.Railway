using TplDataflow.Extensions.Example.BusinessObjects;

namespace TplDataflow.Extensions.Example.Interfaces
{
    /// <summary>
    /// Manages <see cref="EventSetProcessType"/> entities.
    /// </summary>
    public interface IEventSetProcessTypeManager
    {
        /// <summary>
        /// Gets the type of the processing.
        /// </summary>
        /// <param name="eventTypeId">The event type identifier.</param>
        /// <param name="category">The category.</param>
        /// <returns>Suitable process type.</returns>
        EventSetProcessType GetProcessType(int eventTypeId, EventTypeCategory category);
    }
}