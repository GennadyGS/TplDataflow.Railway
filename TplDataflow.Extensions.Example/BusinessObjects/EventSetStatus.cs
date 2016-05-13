// ==========================================================
//  Title: Common.Interface
//  Description: Represents state of the notification.
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
    /// Represents state of the notification.
    /// </summary>
    public enum EventSetStatus : byte
    {
        /// <summary>
        /// The notification has been just created.
        /// </summary>
        New = 1,

        /// <summary>
        /// The notification was accepted by a user.
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// The notification was rejected by a user.
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// A user completed the notification diagnostic.
        /// </summary>
        Completed = 4
    }
}