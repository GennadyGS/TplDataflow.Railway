using System.Runtime.Serialization;

namespace TplDataflow.Extensions.Example.BusinessObjects
{
    /// <summary>
    /// DTO which represents newly created event set.
    /// </summary>
    [DataContract(Name = "escn")]
    public class EventSetAppearingNotification
    {
        /// <summary>
        /// Gets or sets the Id property value.
        /// </summary>
        [DataMember(Name = "id")]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the ResourceId property value.
        /// </summary>
        [DataMember(Name = "ri")]
        public int ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the event type identifier.
        /// </summary>
        [DataMember(Name = "et")]
        public int EventTypeId { get; set; }
    }
}