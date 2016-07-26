using System.Threading.Tasks;
using EventProcessing.BusinessObjects;
using EventProcessing.Interfaces;

namespace EventProcessing.Tests
{
    internal class ProcessTypManagerAsyncProxy : IEventSetProcessTypeManager
    {
        private readonly IEventSetProcessTypeManager _innerManager;

        public ProcessTypManagerAsyncProxy(IEventSetProcessTypeManager innerManager)
        {
            _innerManager = innerManager;
        }

        EventSetProcessType IEventSetProcessTypeManager.GetProcessType(int eventTypeId, EventTypeCategory category)
        {
            return _innerManager.GetProcessTypeAsync(eventTypeId, category).Result;
        }

        Task<EventSetProcessType> IEventSetProcessTypeManager.GetProcessTypeAsync(int eventTypeId, EventTypeCategory category)
        {
            return _innerManager.GetProcessTypeAsync(eventTypeId, category);
        }
    }
}