using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal static class DocumentDraftTestData
{
    public static DocumentDraft Create(DateTime createdAt)
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
            new Uri("/logo.svg", UriKind.Relative),
            createdAt);

        return result.Value;
    }
}
