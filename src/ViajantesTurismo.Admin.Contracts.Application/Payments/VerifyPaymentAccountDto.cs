using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Explicitly confirms an independent check, including self-verification.</summary>
public sealed record VerifyPaymentAccountDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(1, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets Method.</summary>
    public required PaymentAccountVerificationMethodDto Method
    {
        get; init;
    }
    /// <summary>Gets CheckedAt.</summary>
    public required DateTimeOffset CheckedAt
    {
        get; init;
    }
    /// <summary>Gets IndependentCheckAttested.</summary>
    public required bool IndependentCheckAttested
    {
        get; init;
    }
    /// <summary>Gets Confirmed.</summary>
    public required bool Confirmed
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "VerifyPaymentAccountDto [redacted]";
}
