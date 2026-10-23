using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Records an optional independent review for the expected revision.</summary>
public sealed record ReviewPaymentAccountDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(1, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets Outcome.</summary>
    public required PaymentAccountReviewOutcomeDto Outcome
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "ReviewPaymentAccountDto [redacted]";
}
