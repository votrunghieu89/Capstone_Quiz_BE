namespace Capstone.ENUMs
{
    public enum CVStatusEnum
    {
        Pending = 0,
        Approve = 1,
        Reject = 2
    }
    public enum SubmitResult
    {
        Success,
        AlreadyPending,
        AlreadyApproved,
        Error
    }
}
