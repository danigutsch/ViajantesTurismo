using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Test data for regenerated document revisions that need manual review.</summary>
internal static class DocumentManualReviewTestData
{
    private static readonly DateTime Now = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Creates a store holding a regenerated revision under review whose source changed while the template stays current.</summary>
    public static (FakeDocumentStore Store, DocumentDraft Revision) CreateStoreWithChangedRevision()
    {
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();
        var revision = lineage.CreateRevision(
            lineage.Revisions.ShouldHaveSingleItem().Id,
            DocumentLineageTestData.CreateContent(sourceVersion: "SOURCE-VERSION-2"),
            Now,
            context).Value;
        lineage.BeginReview(revision.Id, Now, context).IsSuccess.ShouldBeTrue();
        lineage.ClearDomainEvents();
        var store = new FakeDocumentStore();
        store.Add(lineage);
        return (store, revision);
    }
}
