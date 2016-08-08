using System;

namespace EventProcessing.BusinessObjects
{
    public class EventSetUpdateNotification
    {
        public long Id { get; set; }

        public int ResourceId { get; set; }

        public byte Level { get; set; }

        public byte Status { get; set; }

        public string Owner { get; set; }

        public DateTime? LastReadTime { get; set; }

        public DateTime EventLastReadTime { get; set; }

        public int? CountDelta { get; set; }

        public string Comment { get; set; }

        public string CompletedNote { get; set; }

        public string AcceptedBy { get; set; }

        public string FailureMode { get; set; }

        public DateTime? AcceptedTime { get; set; }

        public DateTime? CompletedTime { get; set; }

        public double? Value { get; set; }

        public double? AssociatedParameterValue { get; set; }
    }
}