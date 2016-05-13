// ==========================================================
//  Title: EventHandling.DataModel
//  Description: Represents EventSetProcessTypeUpdateCriteria.
//  Copyright © 2004-2015 Modular Mining Systems, Inc.
//  All Rights Reserved
// ==========================================================
//  The information described in this document is furnished as proprietary
//  information and may not be copied or sold without the written permission
//  of Modular Mining Systems, Inc.
// ==========================================================

using System;

namespace TplDataflow.Extensions.Example.BusinessObjects
{
    /// <summary>
    /// Represents EventSetProcessTypeUpdateCriteria.
    /// </summary>
    public class EventSetProcessTypeUpdateCriteria
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the Level property value.
        /// </summary>
        public EventLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the Category property value.
        /// </summary>
        public EventTypeCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the AcceptanceRequired property value.
        /// </summary>
        public bool AcceptanceRequired { get; set; }

        /// <summary>
        /// Gets or sets the DiagnosticsRequired property value.
        /// </summary>
        public bool DiagnosticsRequired { get; set; }

        /// <summary>
        /// Gets or sets the AutoComplete property value.
        /// </summary>
        public bool AutoComplete { get; set; }

        /// <summary>
        /// Gets or sets the AutoCompleteTimeout property value.
        /// </summary>
        public TimeSpan? AutoCompleteTimeout { get; set; }

        /// <summary>
        /// Gets or sets the Threshold property value.
        /// </summary>
        public TimeSpan Threshold { get; set; }

        /// <summary>
        /// Gets or sets the PlaySound property value.
        /// </summary>
        public bool PlaySound { get; set; }

        /// <summary>
        /// Gets or sets the ShowToastMessage property value.
        /// </summary>
        public bool ShowToastMessage { get; set; }
    }
}
