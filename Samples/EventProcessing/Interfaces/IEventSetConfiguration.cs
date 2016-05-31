using System;

namespace EventProcessing.Interfaces
{
    /// <summary>
    /// Interface for configuring EventSets.
    /// </summary>
    public interface IEventSetConfiguration
    {
        TimeSpan EventBatchTimeout { get; }

        int EventBatchSize { get; }

        TimeSpan EventGroupBatchTimeout { get; }

        int EventGroupBatchSize { get; }
    }
}