using System;

namespace EventProcessing.BusinessObjects
{
    public class EventArrivedNotification
    {
        public long Id { get; set; }

        public EventTypeCategory Category { get; set; }

        public int ResourceId { get; set; }

        public long EventSetId { get; set; }

        public int EventTypeId { get; set; }

        public DateTimeOffset ReadTime { get; set; }

        public float HeadingAngle { get; set; }

        public double? Value { get; set; }

        public string StringValue { get; set; }

        public double? AssociatedParameterValue { get; set; }

        public string AssociatedParameterStringValue { get; set; }

        public long? AssociatedParameterTypeId { get; set; }

        public string AssociatedParameterTypeName { get; set; }

        public int? GeographicRegionId { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }
    }
}