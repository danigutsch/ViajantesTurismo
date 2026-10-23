namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>An immutable recipient revision, including its historical verification evidence.</summary>
public sealed class PaymentAccountRevision
{
    private PaymentAccountRevision(PaymentAccountRevisionState state, PaymentRecipientDetails details)
    {
        State = state;
        Recipient = details;
    }

    internal PaymentAccountRevisionState State
    {
        get;
    }

    /// <summary>Gets the immutable revision identifier.</summary>
    public Guid Id => State.Id;

    /// <summary>Gets the monotonically increasing revision number.</summary>
    public int Number => State.Number;

    /// <summary>Gets the sensitive validated recipient details.</summary>
    public PaymentRecipientDetails Recipient
    {
        get;
    }

    /// <summary>Gets the opaque revision creator identifier.</summary>
    public string CreatedBy => State.CreatedBy;

    /// <summary>Gets the UTC creation instant.</summary>
    public DateTimeOffset CreatedAt => State.CreatedAt;

    /// <summary>Gets the opaque verifying Admin identifier, when verified.</summary>
    public string? VerifiedBy => State.VerifiedBy;

    /// <summary>Gets the UTC explicit verification instant.</summary>
    public DateTimeOffset? VerifiedAt => State.VerifiedAt;

    /// <summary>Gets the independently checked method.</summary>
    public PaymentAccountVerificationMethod VerificationMethod => State.Method;

    /// <summary>Gets the attested check instant.</summary>
    public DateTimeOffset? CheckedAt => State.CheckedAt;

    /// <summary>Gets whether the entering Admin also explicitly verified this revision.</summary>
    public bool IsSelfApproved => VerifiedBy is not null && string.Equals(CreatedBy, VerifiedBy, StringComparison.Ordinal);

    internal static PaymentAccountRevision FromState(PaymentAccountRevisionState state, PaymentRecipientDetails details) => new(state, details);

    /// <inheritdoc />
    public override string ToString() => "PaymentAccountRevision [redacted]";
}
