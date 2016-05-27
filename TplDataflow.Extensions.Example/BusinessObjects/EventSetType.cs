using System;
using System.Linq;

namespace TplDataflow.Extensions.Example.BusinessObjects
{
    /// <summary>
    /// Represents type of event set
    /// </summary>
    public struct EventSetType : IEquatable<EventSetType>
    {
        /// <summary>
        /// Gets or sets the event type identifier.
        /// </summary>
        public int EventTypeId { get; set; }

        /// <summary>
        /// Gets or sets the event type category.
        /// </summary>
        public EventTypeCategory EventTypeCategory { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public int ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the resource category.
        /// </summary>
        public byte ResourceCategory { get; set; }

        /// <summary>
        /// Gets or sets the site identifier.
        /// </summary>
        public byte SiteId { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        public EventLevel Level { get; set; }

        /// <summary>
        /// Creates from event and level.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="level">The level.</param>
        /// <returns></returns>
        public static EventSetType CreateFromEventAndLevel(EventDetails @event, EventLevel level)
        {
            return new EventSetType
            {
                EventTypeId = @event.EventTypeId,
                EventTypeCategory = @event.Category,
                ResourceId = @event.ResourceId,
                ResourceCategory = @event.ResourceCategory,
                SiteId = @event.SiteId,
                Level = level
            };
        }

        /// <summary>
        /// Gets the code.
        /// </summary>
        /// <returns>The code.</returns>
        public long GetCode()
        {
            const int halfByteSize = 4;

            var bytes = BitConverter.GetBytes((int)EventTypeId)
                .Concat(BitConverter.GetBytes((short)ResourceId))
                .Concat(new[]
                {
                    (byte)((byte)EventTypeCategory << halfByteSize | ResourceCategory),
                    (byte)((byte)Level << halfByteSize | SiteId)
                })
                .ToArray();
            return BitConverter.ToInt64(bytes, 0);
        }

        /// <summary> Serves as the default hash function. </summary>
        /// <returns> A hash code for the current object. </returns>
        public override int GetHashCode()
        {
            var int32Size = 32;
            long code = GetCode();
            return (int)(code >> int32Size) ^ (int)code;
        }

        /// <summary> Indicates whether the current object is equal to another object of the same type. </summary>
        /// <returns> true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false. </returns>
        /// <param name="other"> An object to compare with this object. </param>
        public bool Equals(EventSetType other)
        {
            return SiteId == other.SiteId
                   && Level == other.Level
                   && EventTypeCategory == other.EventTypeCategory
                   && EventTypeId == other.EventTypeId
                   && ResourceCategory == other.ResourceCategory
                   && ResourceId == other.ResourceId;
        }

        /// <summary> Determines whether the specified object is equal to the current object. </summary>
        /// <returns> true if the specified object  is equal to the current object; otherwise, false. </returns>
        /// <param name="otherObj"> The object to compare with the current object. </param>
        public override bool Equals(object otherObj)
        {
            return otherObj is EventSetType && Equals((EventSetType)otherObj);
        }
    }
}