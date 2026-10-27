using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Editable form state for a payment account and its recipient details.</summary>
internal sealed class PaymentAccountFormModel
{
    /// <summary>Display label identifying the account.</summary>
    [Required, StringLength(128)] public string Label { get; set; } = string.Empty;
    /// <summary>Name of the account holder receiving payments.</summary>
    [Required, StringLength(128)] public string RecipientName { get; set; } = string.Empty;
    /// <summary>Account kind that decides which recipient fields apply.</summary>
    public PaymentAccountKindDto Kind { get; set; } = PaymentAccountKindDto.Iban;
    /// <summary>Currency the account settles in; only editable for IBAN accounts.</summary>
    public CurrencyDto SettlementCurrency { get; set; } = CurrencyDto.Euro;
    /// <summary>IBAN, used when the kind is IBAN.</summary>
    public string Iban { get; set; } = string.Empty;
    /// <summary>BIC/SWIFT code, used when the kind is IBAN.</summary>
    public string Bic { get; set; } = string.Empty;
    /// <summary>Type of the Pix key, used when the kind is Pix.</summary>
    public PixKeyTypeDto PixKeyType { get; set; } = PixKeyTypeDto.Email;
    /// <summary>Pix key value, used when the kind is Pix.</summary>
    public string PixKey { get; set; } = string.Empty;
    /// <summary>ISPB bank identifier, used for Brazilian bank accounts.</summary>
    public string Ispb { get; set; } = string.Empty;
    /// <summary>Optional bank branch, used for Brazilian bank accounts.</summary>
    public string Branch { get; set; } = string.Empty;
    /// <summary>Bank account number, used for Brazilian bank accounts.</summary>
    public string AccountNumber { get; set; } = string.Empty;
    /// <summary>Checking or savings type, used for Brazilian bank accounts.</summary>
    public BrazilianAccountTypeDto AccountType { get; set; } = BrazilianAccountTypeDto.Checking;
    /// <summary>Recipient tax identifier, used for Brazilian bank accounts.</summary>
    public string TaxId { get; set; } = string.Empty;
    /// <summary>Builds the recipient DTO, keeping only the fields that apply to the selected kind.</summary>
    public PaymentRecipientDto Recipient() => new()
    {
        Kind = Kind,
        RecipientName = RecipientName,
        SettlementCurrency = Kind == PaymentAccountKindDto.Iban ? SettlementCurrency : CurrencyDto.Real,
        Iban = Kind == PaymentAccountKindDto.Iban ? Iban : null,
        Bic = Kind == PaymentAccountKindDto.Iban ? Bic : null,
        PixKey = Kind == PaymentAccountKindDto.Pix ? PixKey : null,
        PixKeyType = Kind == PaymentAccountKindDto.Pix ? PixKeyType : PixKeyTypeDto.None,
        Ispb = Kind == PaymentAccountKindDto.BrazilianBank ? Ispb : null,
        Branch = Kind == PaymentAccountKindDto.BrazilianBank && !string.IsNullOrWhiteSpace(Branch) ? Branch : null,
        AccountNumber = Kind == PaymentAccountKindDto.BrazilianBank ? AccountNumber : null,
        TaxId = Kind == PaymentAccountKindDto.BrazilianBank ? TaxId : null,
        AccountType = Kind == PaymentAccountKindDto.BrazilianBank ? AccountType : BrazilianAccountTypeDto.None
    };
    /// <summary>Creates a form model from existing payment account details, mapping null values to empty strings.</summary>
    public static PaymentAccountFormModel From(GetPaymentAccountDetailsDto dto) => new()
    {
        Label = dto.Account.Label,
        RecipientName = dto.Recipient.RecipientName,
        Kind = dto.Recipient.Kind,
        SettlementCurrency = dto.Recipient.SettlementCurrency,
        Iban = dto.Recipient.Iban ?? string.Empty,
        Bic = dto.Recipient.Bic ?? string.Empty,
        PixKeyType = dto.Recipient.PixKeyType,
        PixKey = dto.Recipient.PixKey ?? string.Empty,
        Ispb = dto.Recipient.Ispb ?? string.Empty,
        Branch = dto.Recipient.Branch ?? string.Empty,
        AccountNumber = dto.Recipient.AccountNumber ?? string.Empty,
        AccountType = dto.Recipient.AccountType,
        TaxId = dto.Recipient.TaxId ?? string.Empty
    };
    public override string ToString() => "PaymentAccountFormModel [redacted]";
}
