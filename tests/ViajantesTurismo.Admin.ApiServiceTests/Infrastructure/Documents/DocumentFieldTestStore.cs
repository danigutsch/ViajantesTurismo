using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

/// <summary>Supplies one document lineage with a known editable field for API tests.</summary>
internal sealed class DocumentFieldTestStore(DocumentLineage lineage) : IDocumentStore
{
    /// <summary>Gets the seeded document revision ID.</summary>
    internal Guid DocumentId => lineage.Revisions.ShouldHaveSingleItem().Id;

    /// <summary>Creates a single draft with an editable greeting.</summary>
    internal static DocumentFieldTestStore Create()
    {
        var field = DocumentField.Create("greeting", "Greeting", "Hello", DocumentPrivacyClassification.PersonalData, true).Value;
        var content = new DocumentDraftContent(
            "tour-service-contract",
            "1",
            "test-source",
            [field],
            "test-branding",
            "Test Operator",
            null,
            "#102030",
            "#405060",
            "#ffffff",
            "#111111",
            "serif",
            "sans-serif",
            "Test Operator")
        {
            BrandingSourceRevision = 1
        };
        var created = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            content,
            new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc),
            DocumentAuditContext.Create("synthetic-staff", Guid.CreateVersion7().ToString("N")).Value);
        var document = created.Value;
        document.ClearDomainEvents();
        return new DocumentFieldTestStore(document);
    }

    /// <inheritdoc />
    public void Add(DocumentLineage document) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct) =>
        Task.FromResult(documentId == DocumentId ? lineage : null);

    /// <inheritdoc />
    public Task<DocumentPurgeCounts> PurgeExpiredDrafts(DateTime now, HeldDocumentLineages heldLineages, CancellationToken ct) =>
        throw new NotSupportedException();
}
