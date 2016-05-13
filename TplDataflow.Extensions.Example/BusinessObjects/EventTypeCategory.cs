// ==========================================================
//  Title: Common.Interface
//  Description: Represent event category.
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
    /// Represent event category.
    /// </summary>
    public enum EventTypeCategory : byte
    {
        /// <summary>
        /// The OEM Interfaces event.
        /// </summary>
        OemEvent = 1,

        /// <summary>
        /// The user defined event.
        /// </summary>
        UdfEvent = 2,

        /// <summary>
        /// The prediction event.
        /// </summary>
        Prediction = 3,

        /// <summary>
        /// The expiration event.
        /// </summary>
        ExpirationEvent = 4,
    }
}