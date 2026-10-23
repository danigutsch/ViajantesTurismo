using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Records actual gross, fees and net settlement pending explicit EUR reconciliation.</summary>
public sealed record CreateBookingReceiptDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets SettlementCurrency.</summary>
    public required CurrencyDto SettlementCurrency
    {
        get; init;
    }
    /// <summary>Gets Gross.</summary>
    public required decimal Gross
    {
        get; init;
    }
    /// <summary>Gets Fees.</summary>
    public required decimal Fees
    {
        get; init;
    }
    /// <summary>Gets PaymentDate.</summary>
    public required DateOnly PaymentDate
    {
        get; init;
    }
    /// <summary>Gets SettledAt.</summary>
    public DateTimeOffset? SettledAt
    {
        get; init;
    }
    /// <summary>Gets Method.</summary>
    public required PaymentMethodDto Method
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
    /// <summary>Gets ProposedAllocations.</summary>
    [Required]
    public required IReadOnlyList<InstallmentAllocationDto> ProposedAllocations
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "CreateBookingReceiptDto [redacted]";
}
