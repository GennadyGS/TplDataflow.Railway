// ==========================================================
//  Title: Central.Interface
//  Description: Processes EventSet.
//  Copyright © 2004-2014 Modular Mining Systems, Inc.
//  All Rights Reserved
// ==========================================================
//  The information described in this document is furnished as proprietary
//  information and may not be copied or sold without the written permission
//  of Modular Mining Systems, Inc.
// ==========================================================

using System;
using System.Threading.Tasks;
using TplDataflow.Extensions.Example.BusinessObjects;

namespace TplDataflow.Extensions.Example.Interfaces
{
    /// <summary>
    /// Stores instances of <see cref="EventSet" /> in database.
    /// </summary>
    public interface IEventSetStorageProcessor
    {
        IObserver<EventDetails> Input { get; }

        IObservable<EventSetWithEvents> EventSetCreatedOutput { get; }

        IObservable<EventSetWithEvents> EventSetUpdatedOutput { get; }

        IObservable<EventDetails> EventSkippedOutput { get; }

        IObservable<EventDetails> EventFailedOutput { get; }

        Task CompletionTask { get; }
    }
}