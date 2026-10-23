using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Explicitly retires the account and blocks affected instruction delivery.</summary>
public sealed record RetirePaymentAccountDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(1, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets Reason.</summary>
    public required PaymentAccountReasonDto Reason
    {
        get; init;
    }
    /// <summary>Gets Confirmed.</summary>
    public required bool Confirmed
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "RetirePaymentAccountDto [redacted]";
}
