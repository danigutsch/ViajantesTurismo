namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains masked recipient information and approval/review metadata.</summary>
public sealed record GetPaymentAccountDto
{
    /// <summary>Gets the account settlement currency.</summary>
    public required CurrencyDto SettlementCurrency
    {
        get; init;
    }
    /// <summary>Gets Id.</summary>
    public required Guid Id
    {
        get; init;
    }
    /// <summary>Gets Version.</summary>
    public required long Version
    {
        get; init;
    }
    /// <summary>Gets Label.</summary>
    public required string Label
    {
        get; init;
    }
    /// <summary>Gets Status.</summary>
    public required PaymentAccountStatusDto Status
    {
        get; init;
    }
    /// <summary>Gets Kind.</summary>
    public required PaymentAccountKindDto Kind
    {
        get; init;
    }
    /// <summary>Gets RevisionId.</summary>
    public required Guid RevisionId
    {
        get; init;
    }
    /// <summary>Gets Revision.</summary>
    public required int Revision
    {
        get; init;
    }
    /// <summary>Gets MaskedIdentifier.</summary>
    public required string MaskedIdentifier
    {
        get; init;
    }
    /// <summary>Gets CreatedBy.</summary>
    public required string CreatedBy
    {
        get; init;
    }
    /// <summary>Gets CreatedAt.</summary>
    public required DateTimeOffset CreatedAt
    {
        get; init;
    }
    /// <summary>Gets VerifiedBy.</summary>
    public string? VerifiedBy
    {
        get; init;
    }
    /// <summary>Gets VerifiedAt.</summary>
    public DateTimeOffset? VerifiedAt
    {
        get; init;
    }
    /// <summary>Gets VerificationMethod.</summary>
    public required PaymentAccountVerificationMethodDto VerificationMethod
    {
        get; init;
    }
    /// <summary>Gets CheckedAt.</summary>
    public DateTimeOffset? CheckedAt
    {
        get; init;
    }
    /// <summary>Gets SelfApproved.</summary>
    public required bool SelfApproved
    {
        get; init;
    }
    /// <summary>Gets ReviewedBy.</summary>
    public string? ReviewedBy
    {
        get; init;
    }
    /// <summary>Gets ReviewedAt.</summary>
    public DateTimeOffset? ReviewedAt
    {
        get; init;
    }
    /// <summary>Gets ReviewOutcome.</summary>
    public required PaymentAccountReviewOutcomeDto ReviewOutcome
    {
        get; init;
    }
    /// <summary>Gets RequiresAttention.</summary>
    public required bool RequiresAttention
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetPaymentAccountDto [redacted]";
}
