//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;

namespace TplDataflow.Extensions.Example.BusinessObjects
{
    /// <summary>
    /// The auto-generated <see cref="EventSetProcessType"/> entity.
    /// </summary>
    public partial class EventSetProcessType
    {
    	
    	/// <summary>
    	/// Gets or sets the Level property value.
    	/// </summary>
        public byte Level { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the Category property value.
    	/// </summary>
        public byte Category { get; set; }
    	
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
        public Nullable<System.TimeSpan> AutoCompleteTimeout { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the Threshold property value.
    	/// </summary>
        public System.TimeSpan Threshold { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the PlaySound property value.
    	/// </summary>
        public byte PlaySound { get; set; }
    	
    	/// <summary>
    	/// Gets or sets the ShowToastMessage property value.
    	/// </summary>
        public bool ShowToastMessage { get; set; }
    }
}