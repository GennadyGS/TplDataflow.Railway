using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventProcessing.Interfaces
{
    /// <summary>
    /// Provides access to identity management.
    /// </summary>
    public interface IIdentityManagementService
    {
        /// <summary>
        /// Gets the next long ids.
        /// </summary>
        /// <param name="sequenceName">Name of the sequence.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>Batch of generated ids.</returns>
        Task<IList<long>> GetNextLongIdsAsync(string sequenceName, int amount);
    }
}