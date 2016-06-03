using System;

namespace EventProcessing.BusinessObjects
{
    /// <summary>
    /// The auto-generated <see cref="EventSet"/> entity.
    /// </summary>
    public partial class EventSet
    {
    	
    	/// <summary>
    	/// Gets or sets the Id property value.
    	/// </summary>
        public long Id { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the SiteId property value.
    	/// </summary>
        public byte SiteId { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the ResourceCategory property value.
    	/// </summary>
        public byte ResourceCategory { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the ResourceId property value.
    	/// </summary>
        public int ResourceId { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the EventTypeId property value.
    	/// </summary>
        public int EventTypeId { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the EventTypeCategory property value.
    	/// </summary>
        public byte EventTypeCategory { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the Level property value.
    	/// </summary>
        public byte Level { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the Status property value.
    	/// </summary>
        public byte Status { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the Owner property value.
    	/// </summary>
        public string Owner { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the AcceptedTime property value.
    	/// </summary>
        public Nullable<System.DateTime> AcceptedTime { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the CompletedTime property value.
    	/// </summary>
        public Nullable<System.DateTime> CompletedTime { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the CompletedNote property value.
    	/// </summary>
        public string CompletedNote { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the Comment property value.
    	/// </summary>
        public string Comment { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the FailureMode property value.
    	/// </summary>
        public string FailureMode { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the FirstReadTime property value.
    	/// </summary>
        public System.DateTime FirstReadTime { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the LastReadTime property value.
    	/// </summary>
        public System.DateTime LastReadTime { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the LastUpdateTime property value.
    	/// </summary>
        public System.DateTime LastUpdateTime { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the CreationTime property value.
    	/// </summary>
        public System.DateTime CreationTime { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the AcceptedBy property value.
    	/// </summary>
        public string AcceptedBy { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the EventsCount property value.
    	/// </summary>
        public int EventsCount { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the TypeCode property value.
    	/// </summary>
        public long TypeCode { get; set; }
    }
}