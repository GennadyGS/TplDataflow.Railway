// ==========================================================
//  Title: IdentityManagement.Central.Implementation
//  Description: Provides access to identity management.
//  Copyright © 2004-2015 Modular Mining Systems, Inc.
//  All Rights Reserved
// ==========================================================
//  The information described in this document is furnished as proprietary
//  information and may not be copied or sold without the written permission
//  of Modular Mining Systems, Inc.
// ==========================================================

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