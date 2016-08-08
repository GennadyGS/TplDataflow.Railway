using System;

namespace EventProcessing.BusinessObjects
{
    public class EventDetails
    {
        public long Id { get; set; }

        public byte SiteId { get; set; }

        public byte ResourceCategory { get; set; }

        public EventTypeCategory Category { get; set; }

        public int ResourceId { get; set; }

        public int EventTypeId { get; set; }

        public int? AssociatedParameterTypeId { get; set; }

        public string AssociatedParameterTypeName { get; set; }

        public DateTime ReadTime { get; set; }

        public double? Value { get; set; }

        public string StringValue { get; set; }

        public double? AssociatedParameterValue { get; set; }

        public string AssociatedParameterStringValue { get; set; }

        public int? GeographicRegionId { get; set; }

        public double? X { get; set; }

        public double? Y { get; set; }

        public double? Z { get; set; }
    }
}