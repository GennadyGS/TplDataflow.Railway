// ==========================================================
//  Title: Central.Implementation
//  Description: Provides access to the EventSet in DB.
//  Copyright © 2004-2014 Modular Mining Systems, Inc.
//  All Rights Reserved
// ==========================================================
//  The information described in this document is furnished as proprietary
//  information and may not be copied or sold without the written permission
//  of Modular Mining Systems, Inc.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TplDataflow.Extensions.Example.BusinessObjects;

namespace TplDataflow.Extensions.Example.Interfaces
{
    /// <summary>
    /// Provides access to the EventSet in DB.
    /// </summary>
    public interface IEventSetRepository : IDisposable
    {
        /// <summary>
        /// Finds the last event sets by type codes.
        /// </summary>
        /// <param name="typeCodes">The type codes.</param>
        /// <returns>The last event sets by specified type codes</returns>
        IList<EventSet> FindLastEventSetsByTypeCodes(IList<long> typeCodes);

        /// <summary>
        /// Applies the changes to repository including create and update operations.
        /// </summary>
        /// <param name="createdEventSets">The created event sets.</param>
        /// <param name="updatedEventSets">The updated event sets.</param>
        void ApplyChanges(IList<EventSet> createdEventSets, IList<EventSet> updatedEventSets);
   }
}