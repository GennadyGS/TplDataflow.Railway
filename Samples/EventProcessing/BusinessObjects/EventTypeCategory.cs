namespace EventProcessing.BusinessObjects
{
    public enum EventTypeCategory : byte
    {
        OemEvent = 1,

        UdfEvent = 2,

        Prediction = 3,

        ExpirationEvent = 4,
    }
}