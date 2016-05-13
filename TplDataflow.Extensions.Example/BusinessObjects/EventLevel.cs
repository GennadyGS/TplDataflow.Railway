// ==========================================================
//  Title: Common.Interface
//  Description: Represent event criticality level.
//  Copyright © 2004-2014 Modular Mining Systems, Inc.
//  All Rights Reserved
// ==========================================================
//  The information described in this document is furnished as proprietary
//  information and may not be copied or sold without the written permission
//  of Modular Mining Systems, Inc.
// ==========================================================
namespace TplDataflow.Extensions.Example.BusinessObjects
{
    /// <summary>
    /// Represent event criticality level.
    /// </summary>
    public enum EventLevel : byte
    {
        /// <summary>
        /// The non-critical event
        /// </summary>
        NonCritical = 0,

        /// <summary>
        /// The critical event
        /// </summary>
        Critical = 1,

        /// <summary>
        /// The information event
        /// </summary>
        Information = 3
    }
}