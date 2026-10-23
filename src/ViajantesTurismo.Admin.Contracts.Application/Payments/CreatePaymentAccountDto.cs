using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Saves an unverified recipient draft without approval.</summary>
public sealed record CreatePaymentAccountDto
{
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
    public override string ToString() => "CreatePaymentAccountDto [redacted]";
}
