namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Links receipt credit to its original installment, retaining released and refunded history.</summary>
public sealed record GetPaymentAllocationDto
{
    /// <summary>Gets Id.</summary>
    public required Guid Id
    {
        get; init;
    }
    /// <summary>Gets ReceiptId.</summary>
    public required Guid ReceiptId
    {
        get; init;
    }
    /// <summary>Gets InstallmentId.</summary>
    public required Guid InstallmentId
    {
        get; init;
    }
    /// <summary>Gets OriginalEuroCredit.</summary>
    public required decimal OriginalEuroCredit
    {
        get; init;
    }
    /// <summary>Gets ActiveEuroCredit.</summary>
    public required decimal ActiveEuroCredit
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetPaymentAllocationDto [redacted]";
}
