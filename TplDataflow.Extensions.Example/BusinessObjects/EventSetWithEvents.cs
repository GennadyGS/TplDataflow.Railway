using System.Collections.Generic;

namespace TplDataflow.Extensions.Example.BusinessObjects
{
    /// <summary>
    /// Represents tuple containing EventSet ans Event
    /// </summary>
    public class EventSetWithEvents
    {
        /// <summary>
        /// Gets or sets the event set.
        /// </summary>
        public EventSet EventSet { get; set; }

        /// <summary>
        /// Gets or sets the events.
        /// </summary>
        public IList<EventDetails> Events { get; set; }
    }
}