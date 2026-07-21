using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

internal static class DocumentDraftInfrastructureTestData
{
    public static DocumentDraftContent CreateContent(string templateVersion = "1") => new(
        "tour-service-contract",
        templateVersion,
        Guid.CreateVersion7().ToString("N"),
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
        ],
        Guid.CreateVersion7().ToString("N"),
        "Viajantes Turismo",
        new Uri("/logo.svg", UriKind.Relative),
        "#000000",
        "#000000",
        "#ffffff",
        "#000000",
        "system-ui, sans-serif",
        "system-ui, sans-serif",
        "Viajantes Turismo");

    public static DocumentAuditContext CreateAuditContext() => DocumentAuditContext.Create(
        "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
        "9a3ca841b4354928861c660a6e4e1b99").Value;

    public static DocumentLineage CreateLineage(DateTime createdAt, Guid? bookingId = null)
    {
        var result = DocumentLineage.Create(
            bookingId ?? Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            CreateContent(),
            createdAt,
            CreateAuditContext());
        result.IsSuccess.ShouldBeTrue();
        result.Value.ClearDomainEvents();
        return result.Value;
    }

    public static DocumentDraft CreateDraft(DateTime createdAt, Guid? bookingId = null)
    {
        var result = DocumentDraft.Create(
            bookingId ?? Guid.CreateVersion7(),
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

    public static DocumentDraft CreateFinalizedDraft(DateTime createdAt, Guid? bookingId = null)
    {
        var document = CreateDraft(createdAt, bookingId);
        document.BeginReview(createdAt).IsSuccess.ShouldBeTrue();
        document.Approve(createdAt).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), createdAt).IsSuccess.ShouldBeTrue();
        return document;
    }
}
