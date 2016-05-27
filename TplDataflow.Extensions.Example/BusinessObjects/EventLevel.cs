namespace TplDataflow.Extensions.Example.BusinessObjects
{
    /// <summary>
    /// Represent event criticality level.
    /// </summary>
    public enum EventLevel : byte
    {
        /// <summary>
        /// The non-critical event
        /// </summary>
        NonCritical = 0,

        /// <summary>
        /// The critical event
        /// </summary>
        Critical = 1,

        /// <summary>
        /// The information event
        /// </summary>
        Information = 3
    }
}