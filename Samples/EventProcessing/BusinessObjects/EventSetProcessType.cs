using System;

namespace EventProcessing.BusinessObjects
{
    public class EventSetProcessType
    {

        public byte Level { get; set; }

        public byte Category { get; set; }
    	
        public bool AcceptanceRequired { get; set; }
    	
        public bool DiagnosticsRequired { get; set; }
    	
        public bool AutoComplete { get; set; }

        public TimeSpan? AutoCompleteTimeout { get; set; }

        public System.TimeSpan Threshold { get; set; }
    	
        public byte PlaySound { get; set; }
    	
        public bool ShowToastMessage { get; set; }
    }
}
