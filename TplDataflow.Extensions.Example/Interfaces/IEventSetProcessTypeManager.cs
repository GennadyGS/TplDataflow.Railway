// ==========================================================
//  Title: Central.Interface
//  Description: Manages EventSetProcessType entities.
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
    /// Manages <see cref="EventSetProcessType"/> entities.
    /// </summary>
    public interface IEventSetProcessTypeManager
    {
        /// <summary>
        /// Gets the type of the processing.
        /// </summary>
        /// <param name="eventTypeId">The event type identifier.</param>
        /// <param name="category">The category.</param>
        /// <returns>Suitable process type.</returns>
        EventSetProcessType GetProcessType(int eventTypeId, EventTypeCategory category);
    }
}