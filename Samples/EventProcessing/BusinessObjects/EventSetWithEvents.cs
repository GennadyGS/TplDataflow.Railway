using System.Collections.Generic;

namespace EventProcessing.BusinessObjects
{
    public class EventSetWithEvents
    {
        public EventSet EventSet { get; set; }

        public IList<EventDetails> Events { get; set; }
    }
}