using System.Threading.Tasks;
using EventProcessing.BusinessObjects;

namespace EventProcessing.Interfaces
{
    public interface IEventSetProcessTypeManager
    {
        Task<EventSetProcessType> GetProcessTypeAsync(int eventTypeId, EventTypeCategory category);
    }
}