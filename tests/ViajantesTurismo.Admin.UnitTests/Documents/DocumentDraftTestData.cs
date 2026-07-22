using SharedKernel.Branding;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal static class DocumentDraftTestData
{
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

    public static DocumentDraft Create(DateTime createdAt, Uri? brandingLogoUri = null)
    {
        var result = DocumentDraft.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
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
