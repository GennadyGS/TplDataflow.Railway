namespace EventProcessing.BusinessObjects
{
    /// <summary>
    /// Represent event category.
    /// </summary>
    public enum EventTypeCategory : byte
    {
        /// <summary>
        /// The OEM Interfaces event.
        /// </summary>
        OemEvent = 1,

        /// <summary>
        /// The user defined event.
        /// </summary>
        UdfEvent = 2,

        /// <summary>
        /// The prediction event.
        /// </summary>
        Prediction = 3,

        /// <summary>
        /// The expiration event.
        /// </summary>
        ExpirationEvent = 4,
    }
}