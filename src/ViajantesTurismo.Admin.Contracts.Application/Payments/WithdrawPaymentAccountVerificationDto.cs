using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Explicitly withdraws verification without deleting its history.</summary>
public sealed record WithdrawPaymentAccountVerificationDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(1, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets Confirmed.</summary>
    public required bool Confirmed
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "WithdrawPaymentAccountVerificationDto [redacted]";
}
