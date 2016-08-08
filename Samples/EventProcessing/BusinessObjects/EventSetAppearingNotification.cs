namespace EventProcessing.BusinessObjects
{
    public class EventSetAppearingNotification
    {
        public long Id { get; set; }

        public int ResourceId { get; set; }

        public int EventTypeId { get; set; }
    }
}