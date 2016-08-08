using System;

namespace EventProcessing.Interfaces
{
    public interface IEventSetConfiguration
    {
        TimeSpan EventBatchTimeout { get; }

        int EventBatchSize { get; }

        TimeSpan EventGroupBatchTimeout { get; }

        int EventGroupBatchSize { get; }
    }
}