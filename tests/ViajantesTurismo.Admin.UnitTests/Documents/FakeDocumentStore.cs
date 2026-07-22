using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class FakeDocumentStore : IDocumentStore
{
    public List<DocumentDraft> AddedDocuments { get; } = [];

    public Dictionary<Guid, DocumentDraft> Documents { get; } = [];

    public List<DocumentLineage> AddedLineages { get; } = [];

    public DocumentLineage? LastLoadedLineage { get; private set; }

    public void Add(DocumentLineage lineage)
    {
        AddedLineages.Add(lineage);
        foreach (var document in lineage.Revisions)
        {
            AddedDocuments.Add(document);
            Documents.Add(document.Id, document);
        }
    }

    public Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct)
    {
        var targetDocument = Documents.GetValueOrDefault(documentId);
        if (targetDocument is null)
        {
            return Task.FromResult<DocumentLineage?>(null);
        }

        var lineage = AddedLineages.FirstOrDefault(candidate => candidate.Id == targetDocument.DocumentLineageId);
        if (lineage is null)
        {
            lineage = DocumentLineage.Restore(Documents.Values
                .Where(revision => revision.DocumentLineageId == targetDocument.DocumentLineageId));
            AddedLineages.Add(lineage);
        }

        LastLoadedLineage = lineage;
        return Task.FromResult<DocumentLineage?>(lineage);
    }

    public Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct)
    {
        var expired = Documents.Values.Where(document => document.IsExpiredDraft(now)).ToArray();
        foreach (var document in expired)
        {
            AddedDocuments.Remove(document);
            Documents.Remove(document.Id);
        }

        return Task.FromResult(expired.Length);
    }
}
