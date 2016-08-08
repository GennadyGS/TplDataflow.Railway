using System;
using System.Linq;

namespace EventProcessing.BusinessObjects
{
    public struct EventSetType : IEquatable<EventSetType>
    {
        public int EventTypeId { get; set; }

        public EventTypeCategory EventTypeCategory { get; set; }

        public int ResourceId { get; set; }

        public byte ResourceCategory { get; set; }

        public byte SiteId { get; set; }

        public EventLevel Level { get; set; }

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

        public override int GetHashCode()
        {
            var int32Size = 32;
            long code = GetCode();
            return (int)(code >> int32Size) ^ (int)code;
        }

        public bool Equals(EventSetType other)
        {
            return SiteId == other.SiteId
                   && Level == other.Level
                   && EventTypeCategory == other.EventTypeCategory
                   && EventTypeId == other.EventTypeId
                   && ResourceCategory == other.ResourceCategory
                   && ResourceId == other.ResourceId;
        }

        public override bool Equals(object otherObj)
        {
            return otherObj is EventSetType && Equals((EventSetType)otherObj);
        }
    }
}