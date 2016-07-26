using System.Collections.Generic;
using System.Threading.Tasks;
using EventProcessing.Interfaces;

namespace EventProcessing.Tests
{
    internal class IdentityManagementServiceAsyncProxy : IIdentityManagementService
    {
        private readonly IIdentityManagementService _innerService;

        public IdentityManagementServiceAsyncProxy(IIdentityManagementService innerService)
        {
            _innerService = innerService;
        }

        IList<long> IIdentityManagementService.GetNextLongIds(string sequenceName, int amount)
        {
            return _innerService.GetNextLongIdsAsync(sequenceName, amount).Result;
        }

        Task<IList<long>> IIdentityManagementService.GetNextLongIdsAsync(string sequenceName, int amount)
        {
            return _innerService.GetNextLongIdsAsync(sequenceName, amount);
        }
    }
}