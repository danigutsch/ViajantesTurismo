namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains an immutable proposed or approved reconciliation revision.</summary>
public sealed record GetReceiptReconciliationDto
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
    public required IReadOnlyList<InstallmentAllocationDto> Allocations
    {
        get; init;
    }
    /// <summary>Gets Approval.</summary>
    public required FinancialApprovalDto Approval
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetReceiptReconciliationDto [redacted]";
}
