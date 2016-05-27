using System;
using System.Runtime.Serialization;

namespace TplDataflow.Extensions.Example.BusinessObjects
{
    [DataContract(Name = "escn")]
    public class EventArrivedNotification
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [DataMember(Name = "id")]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        [DataMember(Name = "c")]
        public EventTypeCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        [DataMember(Name = "rid")]
        public int ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the event set identifier.
        /// </summary>
        [DataMember(Name = "esid")]
        public long EventSetId { get; set; }

        /// <summary>
        /// Gets or sets the event type identifier.
        /// </summary>
        [DataMember(Name = "etid")]
        public int EventTypeId { get; set; }

        /// <summary>
        /// Gets or sets the read time.
        /// </summary>
        [DataMember(Name = "rt")]
        public DateTimeOffset ReadTime { get; set; }

        public float HeadingAngle { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [DataMember(Name = "v")]
        public double? Value { get; set; }

        /// <summary>
        /// Gets or sets the string value.
        /// </summary>
        [DataMember(Name = "sv")]
        public string StringValue { get; set; }

        /// <summary>
        /// Gets or sets the parameter value.
        /// </summary>
        [DataMember(Name = "av")]
        public double? AssociatedParameterValue { get; set; }

        /// <summary>
        /// Gets or sets the parameter string value.
        /// </summary>
        [DataMember(Name = "asv")]
        public string AssociatedParameterStringValue { get; set; }

        /// <summary>
        /// Gets or sets the associated parameter type identifier.
        /// </summary>
        [DataMember(Name = "apt")]
        public long? AssociatedParameterTypeId { get; set; }

        /// <summary>
        /// Gets or sets the name of the associated parameter type.
        /// </summary>
        [DataMember(Name = "aptn")]
        public string AssociatedParameterTypeName { get; set; }

        /// <summary>
        /// Gets or sets the geographic region identifier.
        /// </summary>
        /// <value>
        /// The geographic region identifier.
        /// </value>
        [DataMember(Name = "grid")]
        public int? GeographicRegionId { get; set; }

        /// <summary>
        /// Gets or sets the X property value.
        /// </summary>
        [DataMember(Name = "x")]
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y property value.
        /// </summary>
        [DataMember(Name = "y")]
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the Z property value.
        /// </summary>
        [DataMember(Name = "z")]
        public double Z { get; set; }
    }
}