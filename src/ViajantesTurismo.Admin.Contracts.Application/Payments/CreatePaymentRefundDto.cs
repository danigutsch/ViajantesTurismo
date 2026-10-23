using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Records an already performed transfer; approval and credit reversal remain separate.</summary>
public sealed record CreatePaymentRefundDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets the actual evidenced transfer amount in the original receipt settlement currency.</summary>
    public required decimal ActualSettlement
    {
        get; init;
    }
    /// <summary>Gets OriginalCashBasis.</summary>
    public required decimal OriginalCashBasis
    {
        get; init;
    }
    /// <summary>Gets EuroCreditReversed.</summary>
    public required decimal EuroCreditReversed
    {
        get; init;
    }
    /// <summary>Gets EuroCashPayable.</summary>
    public required decimal EuroCashPayable
    {
        get; init;
    }
    /// <summary>Gets RefundQuoteId.</summary>
    public Guid? RefundQuoteId
    {
        get; init;
    }
    /// <summary>Gets ReversedAllocations.</summary>
    [Required]
    public required IReadOnlyList<InstallmentAllocationDto> ReversedAllocations
    {
        get; init;
    }
    /// <summary>Gets Reference.</summary>
    [Required]
    [StringLength(128)]
    public required string Reference
    {
        get; init;
    }
    /// <summary>Gets PerformedAt.</summary>
    public required DateTimeOffset PerformedAt
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "CreatePaymentRefundDto [redacted]";
}
