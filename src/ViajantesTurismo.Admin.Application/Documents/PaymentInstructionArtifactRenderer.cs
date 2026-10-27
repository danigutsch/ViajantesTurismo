using System.Globalization;
using PandaBear.DocumentRendering;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Tours;
using RenderField = PandaBear.DocumentRendering.DocumentField;
using RenderPrivacy = PandaBear.DocumentRendering.DocumentPrivacyClassification;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Renders the payment instructions artifact as HTML from the document and its protected snapshot.</summary>
internal static class PaymentInstructionArtifactRenderer
{
    /// <summary>Renders payment terms, verified recipient details and installments as deterministic HTML bytes.</summary>
    public static byte[] Render(DocumentDraft document, PaymentInstructionSnapshot snapshot)
    {
        var terms = snapshot.Terms;
        var currency = terms.Currency == Currency.Euro ? "EUR" : "BRL";
        var fields = new List<RenderField>
        {
            new("Payment reference", terms.Reference, RenderPrivacy.Operational),
            new("EUR obligation selected", Money(terms.EuroAmount) + " EUR", RenderPrivacy.PersonalData),
            new("Settlement currency", currency, RenderPrivacy.Operational),
            new("Amount to transfer", Money(terms.SettlementAmount) + " " + currency, RenderPrivacy.PersonalData),
            new("Recipient", snapshot.Recipient.RecipientName, RenderPrivacy.PersonalData)
        };
        if (terms.Quote is { } quote)
        {
            fields.Add(new RenderField("Agreed BRL per EUR", quote.BrlPerEuro.ToString("0.########", CultureInfo.InvariantCulture), RenderPrivacy.PersonalData));
            fields.Add(new RenderField("Quote reference", quote.Source + " / " + quote.Reference, RenderPrivacy.PersonalData));
            fields.Add(new RenderField("Quote expires (UTC)", quote.ExpiresAt.ToString("O", CultureInfo.InvariantCulture), RenderPrivacy.Operational));
        }
        switch (snapshot.Recipient.Kind)
        {
            case PaymentAccountKind.Iban:
                fields.Add(new RenderField("IBAN", snapshot.Recipient.BankIban?.Value ?? throw new InvalidOperationException("Missing verified IBAN."), RenderPrivacy.PersonalData));
                fields.Add(new RenderField("BIC", snapshot.Recipient.BankBic?.Value ?? throw new InvalidOperationException("Missing verified BIC."), RenderPrivacy.PersonalData));
                break;
            case PaymentAccountKind.Pix:
                var pix = snapshot.Recipient.Pix ?? throw new InvalidOperationException("Missing verified Pix key.");
                fields.Add(new RenderField("Pix key type", pix.Type.ToString(), RenderPrivacy.Operational));
                fields.Add(new RenderField("Pix key", pix.Value, RenderPrivacy.PersonalData));
                break;
            case PaymentAccountKind.BrazilianBank:
                fields.Add(new RenderField("ISPB", snapshot.Recipient.BankIspb ?? string.Empty, RenderPrivacy.PersonalData));
                fields.Add(new RenderField("Branch", snapshot.Recipient.BankBranch ?? "Not required", RenderPrivacy.PersonalData));
                fields.Add(new RenderField("Account including verifier", snapshot.Recipient.BankAccountNumber ?? string.Empty, RenderPrivacy.PersonalData));
                fields.Add(new RenderField("Account type", snapshot.Recipient.BankAccountType.ToString(), RenderPrivacy.PersonalData));
                fields.Add(new RenderField("Holder CPF/CNPJ", snapshot.Recipient.HolderTaxId ?? string.Empty, RenderPrivacy.PersonalData));
                break;
            default:
                throw new InvalidOperationException("Unknown verified recipient format.");
        }
        var sections = new List<DocumentSection> { new("Payment details", fields) };
        foreach (var installment in terms.Installments)
        {
            sections.Add(new DocumentSection("Installment due " + installment.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            [
                new RenderField("EUR credit", Money(installment.EuroAmount) + " EUR", RenderPrivacy.PersonalData),
                new RenderField("Settlement amount", Money(installment.SettlementAmount) + " " + currency, RenderPrivacy.PersonalData),
                new RenderField("Rounding adjustment", Money(installment.RoundingAdjustment) + " " + currency, RenderPrivacy.Operational)
            ]));
        }
        return new HtmlDocumentRenderer().Render(new DocumentRenderRequest("en", "Payment instructions", sections,
            new DocumentBrandingSnapshot(document.BrandingVersion, document.BrandingName, document.BrandingLogoUri is { IsAbsoluteUri: false } uri ? uri : null,
                document.BrandingPrimaryColor, document.BrandingAccentColor, document.BrandingBackgroundColor, document.BrandingTextColor,
                document.BrandingHeadingFontFamily, document.BrandingBodyFontFamily, document.BrandingFooterText)));
    }
    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
