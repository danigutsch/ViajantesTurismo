using ViajantesTurismo.Admin.Application.Idempotency;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Application.Payments;

internal static class PaymentInstructionContentFactory
{
    public static Result<DocumentDraftContent> Create(FrozenPaymentInstruction terms, DocumentBrandingSnapshotValues branding)
    {
        // Ordinary document fields contain no sensitive snapshot payload. Financial fields are rendered only from protected typed snapshots.
        var field = DocumentField.Create("payment-instructions", "Payment details", "Generated from the protected approved schedule and verified recipient snapshot.", DocumentPrivacyClassification.Operational, false);
        if (field.IsFailure)
        {
            return field.ConvertError<DocumentField, DocumentDraftContent>();
        }
        var source = ApplicationRequestFingerprint.Create(terms.BookingId.ToString("N"), terms.ScheduleId.ToString("N"), terms.AccountRevisionId.ToString("N"), terms.Reference, branding.Version);
        return Result.Ok(new DocumentDraftContent("payment-instructions", "1", source, [field.Value], branding.Version, branding.BrandName, branding.LogoUri,
            branding.PrimaryColor, branding.AccentColor, branding.BackgroundColor, branding.TextColor, branding.HeadingFontFamily, branding.BodyFontFamily, branding.FooterText));
    }
}
