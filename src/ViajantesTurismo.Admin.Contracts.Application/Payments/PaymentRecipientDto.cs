using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains sensitive recipient inputs; never include it in telemetry.</summary>
public sealed record PaymentRecipientDto
{
    /// <summary>Gets the independently checked account settlement currency.</summary>
    public required CurrencyDto SettlementCurrency
    {
        get; init;
    }
    /// <summary>Gets Kind.</summary>
    public required PaymentAccountKindDto Kind
    {
        get; init;
    }
    /// <summary>Gets RecipientName.</summary>
    [Required]
    [StringLength(128)]
    public required string RecipientName
    {
        get; init;
    }
    /// <summary>Gets Iban.</summary>
    [StringLength(34)]
    public string? Iban
    {
        get; init;
    }
    /// <summary>Gets Bic.</summary>
    [StringLength(11)]
    public string? Bic
    {
        get; init;
    }
    /// <summary>Gets PixKey.</summary>
    [StringLength(77)]
    public string? PixKey
    {
        get; init;
    }
    /// <summary>Gets Ispb.</summary>
    [StringLength(8)]
    public string? Ispb
    {
        get; init;
    }
    /// <summary>Gets Branch.</summary>
    [StringLength(4)]
    public string? Branch
    {
        get; init;
    }
    /// <summary>Gets AccountNumber.</summary>
    [StringLength(20)]
    public string? AccountNumber
    {
        get; init;
    }
    /// <summary>Gets TaxId.</summary>
    [StringLength(14)]
    public string? TaxId
    {
        get; init;
    }
    /// <summary>Gets PixKeyType.</summary>
    public PixKeyTypeDto PixKeyType
    {
        get; init;
    }
    /// <summary>Gets AccountType.</summary>
    public BrazilianAccountTypeDto AccountType
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "PaymentRecipientDto [redacted]";
}
