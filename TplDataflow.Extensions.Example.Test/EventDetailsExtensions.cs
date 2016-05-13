// ==========================================================
//  Title: Central.Implementation.Test
//  Description: Test for EventSetProcessTypeRepository.
//  Copyright © 2004-2014 Modular Mining Systems, Inc.
//  All Rights Reserved
// ==========================================================
//  The information described in this document is furnished as proprietary
//  information and may not be copied or sold without the written permission
//  of Modular Mining Systems, Inc.
// ==========================================================

using System;
using TplDataflow.Extensions.Example.BusinessObjects;

namespace TplDataflow.Extensions.Example.Test
{
    internal static class EventDetailsExtensions
    {
        public static EventDetails Clone(this EventDetails @this)
        {
            return new EventDetails
            {
                AssociatedParameterStringValue = @this.AssociatedParameterStringValue,
                AssociatedParameterTypeId = @this.AssociatedParameterTypeId,
                AssociatedParameterTypeName = @this.AssociatedParameterTypeName,
                AssociatedParameterValue = @this.AssociatedParameterValue,
                Category = @this.Category,
                EventTypeId = @this.EventTypeId,
                GeographicRegionId = @this.GeographicRegionId,
                Id = @this.Id,
                ReadTime = @this.ReadTime,
                ResourceCategory = @this.ResourceCategory,
                ResourceId = @this.ResourceId,
                SiteId = @this.SiteId,
                StringValue = @this.StringValue,
                Value = @this.Value,
                X = @this.X,
                Y = @this.Y,
                Z = @this.Z
            };
        }

        public static EventDetails WithReadTime(this EventDetails @event, DateTime readTime)
        {
            @event.ReadTime = readTime;
            return @event;
        }
    }
}