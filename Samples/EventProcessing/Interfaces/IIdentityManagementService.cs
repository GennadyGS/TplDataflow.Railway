using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventProcessing.Interfaces
{
    public interface IIdentityManagementService
    {
        Task<IList<long>> GetNextLongIdsAsync(string sequenceName, int amount);
    }
}