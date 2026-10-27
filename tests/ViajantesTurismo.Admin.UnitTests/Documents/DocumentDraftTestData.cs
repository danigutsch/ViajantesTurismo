using PandaBear.Branding;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Test data for document drafts.</summary>
internal static class DocumentDraftTestData
{
    /// <summary>Creates valid branding settings for the Viajantes brand.</summary>
    public static BrandingSettingsDto CreateBrandingSettings() => new()
    {
        BrandName = "Viajantes",
        PrimaryColor = "#102030",
        AccentColor = "#405060",
        BackgroundColor = "#fdfdfd",
        TextColor = "#111111",
        HeadingFontFamily = "Montserrat",
        BodyFontFamily = "Inter"
    };

    /// <summary>Creates a valid draft with a customer-facing contract fixture created at the given time.</summary>
    public static DocumentDraft Create(
        DateTime createdAt,
        Uri? brandingLogoUri = null,
        DocumentType type = DocumentType.BookingConfirmationContract,
        Guid? bookingId = null)
    {
        var result = DocumentDraft.Create(
            bookingId ?? Guid.CreateVersion7(),
            type,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            "SOURCE-VERSION",
            [
                DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
                DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
            ],
            "BRANDING-VERSION",
            "Viajantes Turismo",
            brandingLogoUri ?? new Uri("/logo.svg", UriKind.Relative),
            createdAt);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
