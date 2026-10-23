using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Explicitly assigns or releases selected EUR credit with Admin confirmation.</summary>
public sealed record AllocatePaymentCreditDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets Allocations.</summary>
    [Required]
    public required IReadOnlyList<InstallmentAllocationDto> Allocations
    {
        get; init;
    }
    /// <summary>Gets Reason.</summary>
    public required FinancialReasonDto Reason
    {
        get; init;
    }
    /// <summary>Gets Confirmed.</summary>
    public required bool Confirmed
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "AllocatePaymentCreditDto [redacted]";
}
