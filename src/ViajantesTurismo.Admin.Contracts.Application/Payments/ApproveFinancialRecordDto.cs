using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Confirms the current summary with a required Admin approval reason.</summary>
public sealed record ApproveFinancialRecordDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedVersion
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
    public override string ToString() => "ApproveFinancialRecordDto [redacted]";
}
