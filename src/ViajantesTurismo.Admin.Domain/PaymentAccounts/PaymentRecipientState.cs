using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Persistence snapshot of payment recipient details; fields not used by the recipient kind are null or none.</summary>
/// <remarks>Untrusted persistence data. Restore through <see cref="PaymentRecipientDetails" /> before use.</remarks>
/// <param name="Kind">The recipient account kind.</param>
/// <param name="RecipientName">The recipient name.</param>
/// <param name="Iban">The IBAN, for IBAN recipients.</param>
/// <param name="Bic">The BIC, for IBAN recipients.</param>
/// <param name="PixType">The Pix key type, for Pix recipients.</param>
/// <param name="PixKey">The Pix key, for Pix recipients.</param>
/// <param name="Ispb">The ISPB of the Brazilian bank, for bank recipients.</param>
/// <param name="Branch">The Brazilian bank branch without its check digit.</param>
/// <param name="AccountNumber">The Brazilian account number including its verifier.</param>
/// <param name="AccountType">The Brazilian bank account type.</param>
/// <param name="TaxId">The account holder's CPF or CNPJ.</param>
/// <param name="SettlementCurrency">The currency in which the recipient is paid.</param>
internal sealed record PaymentRecipientState(
    PaymentAccountKind Kind, string RecipientName, string? Iban, string? Bic,
    PandaBear.BuildingBlocks.PixKeyType PixType, string? PixKey, string? Ispb,
    string? Branch, string? AccountNumber, BrazilianAccountType AccountType, string? TaxId, Currency SettlementCurrency)
{
    public override string ToString() => "PaymentRecipientState [redacted]";
}
