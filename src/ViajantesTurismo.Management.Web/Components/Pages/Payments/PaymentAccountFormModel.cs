using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

internal sealed class PaymentAccountFormModel
{
    [Required, StringLength(128)] public string Label { get; set; } = string.Empty;
    [Required, StringLength(128)] public string RecipientName { get; set; } = string.Empty;
    public PaymentAccountKindDto Kind { get; set; } = PaymentAccountKindDto.Iban;
    public CurrencyDto SettlementCurrency { get; set; } = CurrencyDto.Euro;
    public string Iban { get; set; } = string.Empty;
    public string Bic { get; set; } = string.Empty;
    public PixKeyTypeDto PixKeyType { get; set; } = PixKeyTypeDto.Email;
    public string PixKey { get; set; } = string.Empty;
    public string Ispb { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public BrazilianAccountTypeDto AccountType { get; set; } = BrazilianAccountTypeDto.Checking;
    public string TaxId { get; set; } = string.Empty;
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
