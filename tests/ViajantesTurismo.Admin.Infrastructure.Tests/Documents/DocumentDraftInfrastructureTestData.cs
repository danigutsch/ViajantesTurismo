using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

internal static class DocumentDraftInfrastructureTestData
{
    public static DocumentDraft CreateDraft(DateTime createdAt)
    {
        var result = DocumentDraft.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            Guid.CreateVersion7().ToString("N"),
            [
                DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
                DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
            ],
            Guid.CreateVersion7().ToString("N"),
            "Viajantes Turismo",
            new Uri("/logo.svg", UriKind.Relative),
            createdAt);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    public static DocumentDraft CreateFinalizedDraft(DateTime createdAt)
    {
        var document = CreateDraft(createdAt);
        document.BeginReview(createdAt).IsSuccess.ShouldBeTrue();
        document.Approve(createdAt).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), createdAt).IsSuccess.ShouldBeTrue();
        return document;
    }
}
