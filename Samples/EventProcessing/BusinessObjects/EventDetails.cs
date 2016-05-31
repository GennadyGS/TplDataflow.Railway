using System;

namespace EventProcessing.BusinessObjects
{
    /// <summary>
    /// Represents general event details.
    /// </summary>
    public class EventDetails
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the site identifier.
        /// </summary>
        public byte SiteId { get; set; }

        /// <summary>
        /// Gets or sets the resource category.
        /// </summary>
        public byte ResourceCategory { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public EventTypeCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public int ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the event type identifier.
        /// </summary>
        public int EventTypeId { get; set; }

        /// <summary>
        /// Gets or sets the associated parameter type identifier.
        /// </summary>
        public int? AssociatedParameterTypeId { get; set; }

        /// <summary>
        /// Gets or sets the name of the associated parameter type.
        /// </summary>
        public string AssociatedParameterTypeName { get; set; }

        /// <summary>
        /// Gets or sets the read time.
        /// </summary>
        public DateTime ReadTime { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        /// Gets or sets the string value.
        /// </summary>
        public string StringValue { get; set; }

        /// <summary>
        /// Gets or sets the parameter value.
        /// </summary>
        public double? AssociatedParameterValue { get; set; }

        /// <summary>
        /// Gets or sets the parameter string value.
        /// </summary>
        public string AssociatedParameterStringValue { get; set; }

        /// <summary>
        /// Gets or sets the geographic region identifier.
        /// </summary>
        /// <value>
        /// The geographic region identifier.
        /// </value>
        public int? GeographicRegionId { get; set; }

        /// <summary>
        /// Gets or sets the X property value.
        /// </summary>
        public double? X { get; set; }

        /// <summary>
        /// Gets or sets the Y property value.
        /// </summary>
        public double? Y { get; set; }

        /// <summary>
        /// Gets or sets the Z property value.
        /// </summary>
        public double? Z { get; set; }
    }
}