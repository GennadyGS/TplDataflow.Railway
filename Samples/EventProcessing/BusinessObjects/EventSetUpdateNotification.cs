using System;
using System.Runtime.Serialization;

namespace EventProcessing.BusinessObjects
{
    /// <summary>
    /// DTO which represents updated event set.
    /// </summary>
    [DataContract(Name = "esun")]
    public class EventSetUpdateNotification
    {
        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "rid")]
        public int ResourceId { get; set; }

        [DataMember(Name = "lv")]
        public byte Level { get; set; }

        [DataMember(Name = "st")]
        public byte Status { get; set; }

        [DataMember(Name = "own")]
        public string Owner { get; set; }

        [DataMember(Name = "lrt")]
        public DateTime? LastReadTime { get; set; }

        [DataMember(Name = "elrt")]
        public DateTime EventLastReadTime { get; set; }

        [DataMember(Name = "dc")]
        public int? CountDelta { get; set; }

        [DataMember(Name = "c")]
        public string Comment { get; set; }

        [DataMember(Name = "cn")]
        public string CompletedNote { get; set; }

        [DataMember(Name = "aby")]
        public string AcceptedBy { get; set; }

        [DataMember(Name = "fm")]
        public string FailureMode { get; set; }

        [DataMember(Name = "at")]
        public DateTime? AcceptedTime { get; set; }

        [DataMember(Name = "ct")]
        public DateTime? CompletedTime { get; set; }

        [DataMember(Name = "v")]
        public double? Value { get; set; }

        [DataMember(Name = "apv")]
        public double? AssociatedParameterValue { get; set; }
    }
}