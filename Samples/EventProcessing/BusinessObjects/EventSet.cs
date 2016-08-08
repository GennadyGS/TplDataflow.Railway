using System;

namespace EventProcessing.BusinessObjects
{
    public partial class EventSet
    {

        public long Id { get; set; }

        public byte SiteId { get; set; }

        public byte ResourceCategory { get; set; }

        public int ResourceId { get; set; }

        public int EventTypeId { get; set; }

        public byte EventTypeCategory { get; set; }

        public byte Level { get; set; }

        public byte Status { get; set; }

        public string Owner { get; set; }

        public Nullable<System.DateTime> AcceptedTime { get; set; }

        public Nullable<System.DateTime> CompletedTime { get; set; }

        public string CompletedNote { get; set; }

        public string Comment { get; set; }

        public string FailureMode { get; set; }

        public System.DateTime FirstReadTime { get; set; }

        public System.DateTime LastReadTime { get; set; }

        public System.DateTime LastUpdateTime { get; set; }

        public System.DateTime CreationTime { get; set; }

        public string AcceptedBy { get; set; }

        public int EventsCount { get; set; }

        public long TypeCode { get; set; }
    }
}
