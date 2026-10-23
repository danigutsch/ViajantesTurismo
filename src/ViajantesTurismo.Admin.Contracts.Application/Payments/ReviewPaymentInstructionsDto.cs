using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Records optional independent review without silently withdrawing an existing approval.</summary>
public sealed record ReviewPaymentInstructionsDto
{
    /// <summary>Gets the document concurrency version actually reviewed.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedDocumentVersion
    {
        get; init;
    }
    /// <summary>Gets whether attention is requested for this immutable revision.</summary>
    public required bool ChangesRequested
    {
        get; init;
    }
}
