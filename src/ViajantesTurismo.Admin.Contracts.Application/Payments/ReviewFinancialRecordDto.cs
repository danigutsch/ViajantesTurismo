using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Records optional independent review without implicitly revoking approval.</summary>
public sealed record ReviewFinancialRecordDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets ChangesRequested.</summary>
    public required bool ChangesRequested
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "ReviewFinancialRecordDto [redacted]";
}
