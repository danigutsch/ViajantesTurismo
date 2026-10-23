using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

// Untrusted persistence data. Restore through PaymentRecipientDetails before use.
internal sealed record PaymentRecipientState(
    PaymentAccountKind Kind, string RecipientName, string? Iban, string? Bic,
    PandaBear.BuildingBlocks.PixKeyType PixType, string? PixKey, string? Ispb,
    string? Branch, string? AccountNumber, BrazilianAccountType AccountType, string? TaxId, Currency SettlementCurrency)
{
    public override string ToString() => "PaymentRecipientState [redacted]";
}
