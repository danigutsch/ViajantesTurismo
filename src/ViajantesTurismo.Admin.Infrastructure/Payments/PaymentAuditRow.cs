namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Append-only audit row for payment account and financial history operations.</summary>
internal sealed class PaymentAuditRow
{
    /// <summary>The database-generated ordering sequence.</summary>
    public long Sequence
    {
        get; set;
    }
    /// <summary>The audit entry identifier.</summary>
    public Guid Id
    {
        get; set;
    }
    /// <summary>The audited account or booking identifier.</summary>
    public Guid ResourceId
    {
        get; set;
    }
    /// <summary>The related target identifier, when any.</summary>
    public Guid? TargetId
    {
        get; set;
    }
    /// <summary>The resource version or revision at the time.</summary>
    public long Revision
    {
        get; set;
    }
    /// <summary>The actor who performed the operation.</summary>
    public string ActorId { get; set; } = string.Empty;
    /// <summary>When the operation occurred.</summary>
    public DateTimeOffset OccurredAt
    {
        get; set;
    }
    /// <summary>The operation name.</summary>
    public string Operation { get; set; } = string.Empty;
    /// <summary>The reason code.</summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>Whether the actor approved their own action.</summary>
    public bool SelfApproved
    {
        get; set;
    }
}
