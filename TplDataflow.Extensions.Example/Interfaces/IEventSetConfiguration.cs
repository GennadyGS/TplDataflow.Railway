namespace TplDataflow.Extensions.Example.Interfaces
{
    /// <summary>
    /// Interface for configuring EventSets.
    /// </summary>
    public interface IEventSetConfiguration
    {
        string EventBatchTimeout { get; }

        int EventBatchSize { get; }

        string EventGroupBatchTimeout { get; }

        int EventGroupBatchSize { get; }
    }
}