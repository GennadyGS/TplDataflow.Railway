using EventProcessing.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventProcessing.Interfaces
{
    public interface IEventSetRepository : IDisposable
    {
        Task<IList<EventSet>> FindLastEventSetsByTypeCodesAsync(IList<long> typeCodes);

        Task ApplyChangesAsync(IList<EventSet> createdEventSets, IList<EventSet> updatedEventSets);
    }
}