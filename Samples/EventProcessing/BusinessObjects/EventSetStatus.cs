namespace EventProcessing.BusinessObjects
{
    public enum EventSetStatus : byte
    {
        New = 1,

        Accepted = 2,

        Rejected = 3,

        Completed = 4
    }
}