using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal static class DocumentLineageTestData
{
    public static DocumentDraftContent CreateContent(string templateVersion = "1") => new(
        "tour-service-contract",
        templateVersion,
        $"SOURCE-VERSION-{templateVersion}",
        [
            DocumentField.Create(
                "booking-reference",
                "Booking reference",
                "ABC123",
                DocumentPrivacyClassification.Operational,
                false).Value,
            DocumentField.Create(
                "greeting",
                "Greeting",
                "Dear customer",
                DocumentPrivacyClassification.PersonalData,
                true).Value,
        ],
        "BRANDING-VERSION",
        "Viajantes Turismo",
        new Uri("/logo.svg", UriKind.Relative),
        "#102030",
        "#405060",
        "#ffffff",
        "#111111",
        "Montserrat",
        "Inter",
        "Viajantes Turismo");

    public static DocumentLineage Create(Guid? bookingId = null, DateTime? now = null)
    {
        var result = DocumentLineage.Create(
            bookingId ?? Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            CreateContent(),
            now ?? new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
            DocumentAuditTestData.CreateContext());
        result.IsSuccess.ShouldBeTrue();
        result.Value.ClearDomainEvents();
        return result.Value;
    }
}
