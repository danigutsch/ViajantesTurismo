using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Test data for document lineages.</summary>
internal static class DocumentLineageTestData
{
    /// <summary>Creates draft content for the service contract template at the given version.</summary>
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
        DocumentLogoTestData.DataUri,
        "#102030",
        "#405060",
        "#ffffff",
        "#111111",
        "Montserrat",
        "Inter",
        "Viajantes Turismo");

    /// <summary>Creates a valid contract lineage with its creation domain events cleared.</summary>
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
