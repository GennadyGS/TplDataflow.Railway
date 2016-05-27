using System.Collections.Generic;

namespace TplDataflow.Extensions.Example.Interfaces
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
        IList<long> GetNextLongIds(string sequenceName, int amount);
   }
}