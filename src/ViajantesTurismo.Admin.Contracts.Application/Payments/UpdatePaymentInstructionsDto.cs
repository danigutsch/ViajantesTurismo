using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Confirms the current immutable payment instruction summary before an explicit lifecycle action.</summary>
public sealed record UpdatePaymentInstructionsDto
{
    /// <summary>Gets the expected document-lineage concurrency version.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedDocumentVersion
    {
        get; init;
    }
    /// <summary>Gets the bounded action reason.</summary>
    public required FinancialReasonDto Reason
    {
        get; init;
    }
    /// <summary>Gets whether the Admin explicitly confirmed the current summary.</summary>
    public required bool Confirmed
    {
        get; init;
    }
}
