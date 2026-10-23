using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Saves a proposed EUR credit and explicit allocations without settling debt.</summary>
public sealed record CreateReceiptReconciliationDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets EuroCredit.</summary>
    public required decimal EuroCredit
    {
        get; init;
    }
    /// <summary>Gets QuoteId.</summary>
    public Guid? QuoteId
    {
        get; init;
    }
    /// <summary>Gets InstructionId.</summary>
    public Guid? InstructionId
    {
        get; init;
    }
    /// <summary>Gets Allocations.</summary>
    [Required]
    public required IReadOnlyList<InstallmentAllocationDto> Allocations
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "CreateReceiptReconciliationDto [redacted]";
}
