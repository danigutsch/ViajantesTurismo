using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Creates an unverified replacement recipient revision.</summary>
public sealed record UpdatePaymentAccountDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(1, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets Label.</summary>
    [Required]
    [StringLength(128)]
    public required string Label
    {
        get; init;
    }
    /// <summary>Gets Recipient.</summary>
    [Required]
    public required PaymentRecipientDto Recipient
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "UpdatePaymentAccountDto [redacted]";
}
