namespace Dataflow.Core
{
    public interface IGroupedDataflow<out TKey, out TElement> : IDataflow<TElement>
    {
        TKey Key { get; }
    }
}