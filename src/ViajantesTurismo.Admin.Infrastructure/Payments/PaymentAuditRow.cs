namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentAuditRow
{
    public long Sequence
    {
        get; set;
    }
    public Guid Id
    {
        get; set;
    }
    public Guid ResourceId
    {
        get; set;
    }
    public Guid? TargetId
    {
        get; set;
    }
    public long Revision
    {
        get; set;
    }
    public string ActorId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt
    {
        get; set;
    }
    public string Operation { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool SelfApproved
    {
        get; set;
    }
}
