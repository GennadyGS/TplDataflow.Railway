using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventProcessing.BusinessObjects;
using EventProcessing.Interfaces;

namespace EventProcessing.Tests
{
    internal class EventSetRepositoryAsyncProxy : IEventSetRepository
    {
        private readonly IEventSetRepository _innerRepository;

        public EventSetRepositoryAsyncProxy(IEventSetRepository innerRepository)
        {
            _innerRepository = innerRepository;
        }

        void IDisposable.Dispose()
        {
            _innerRepository.Dispose();
        }

        IList<EventSet> IEventSetRepository.FindLastEventSetsByTypeCodes(IList<long> typeCodes)
        {
            return _innerRepository.FindLastEventSetsByTypeCodesAsync(typeCodes).Result;
        }

        Task<IList<EventSet>> IEventSetRepository.FindLastEventSetsByTypeCodesAsync(IList<long> typeCodes)
        {
            return _innerRepository.FindLastEventSetsByTypeCodesAsync(typeCodes);
        }

        void IEventSetRepository.ApplyChanges(IList<EventSet> createdEventSets, IList<EventSet> updatedEventSets)
        {
            _innerRepository.ApplyChangesAsync(createdEventSets, updatedEventSets).Wait();
        }

        Task IEventSetRepository.ApplyChangesAsync(IList<EventSet> createdEventSets, IList<EventSet> updatedEventSets)
        {
            return _innerRepository.ApplyChangesAsync(createdEventSets, updatedEventSets);
        }
    }
}