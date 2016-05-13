// ==========================================================
//  Title: Central.Interface
//  Description: Interface for configuring EventSets.
//  Copyright © 2004-2014 Modular Mining Systems, Inc.
//  All Rights Reserved
// ==========================================================
//  The information described in this document is furnished as proprietary
//  information and may not be copied or sold without the written permission
//  of Modular Mining Systems, Inc.
// ==========================================================

using System;

namespace TplDataflow.Extensions.Example.Interfaces
{
    /// <summary>
    /// Interface for configuring EventSets.
    /// </summary>
    public interface IEventSetConfiguration
    {
        /// <summary>
        /// Gets the autocomplete operation period.
        /// </summary>
        TimeSpan AutoCompleteMonitorSyncPeriod { get; }

        /// <summary>
        /// Gets the amount of event sets to be autocompleted simultaneously.
        /// </summary>
        int AutoCompleteBatchSize { get; }

        int MaxProcessingAttempts { get;  }
    }
}