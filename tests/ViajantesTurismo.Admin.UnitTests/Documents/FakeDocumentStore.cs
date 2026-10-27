using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>In-memory document store fake.</summary>
internal sealed class FakeDocumentStore : IDocumentStore
{
    /// <summary>Document drafts added through lineages.</summary>
    public List<DocumentDraft> AddedDocuments { get; } = [];

    /// <summary>Stored document drafts keyed by document id.</summary>
    public Dictionary<Guid, DocumentDraft> Documents { get; } = [];

    /// <summary>Lineages added to the store.</summary>
    public List<DocumentLineage> AddedLineages { get; } = [];

    /// <summary>Lineage returned by the last successful load, or null when none was loaded.</summary>
    public DocumentLineage? LastLoadedLineage
    {
        get; private set;
    }

    /// <summary>Stores the lineage and indexes each of its revisions.</summary>
    public void Add(DocumentLineage lineage)
    {
        AddedLineages.Add(lineage);
        foreach (var document in lineage.Revisions)
        {
            AddedDocuments.Add(document);
            Documents.Add(document.Id, document);
        }
    }

    /// <summary>Loads the lineage containing the document, restoring it from stored revisions when not added, or null when the document is unknown.</summary>
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

    /// <summary>Removes expired drafts as of the given time and returns how many were removed.</summary>
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
