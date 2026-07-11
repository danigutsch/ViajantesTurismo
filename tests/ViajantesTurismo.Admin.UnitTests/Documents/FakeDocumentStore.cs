using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class FakeDocumentStore : IDocumentStore
{
    public List<DocumentDraft> AddedDocuments { get; } = [];

    public Dictionary<Guid, DocumentDraft> Documents { get; } = [];

    public void Add(DocumentDraft document)
    {
        AddedDocuments.Add(document);
        Documents.Add(document.Id, document);
    }

    public Task<DocumentDraft?> GetById(Guid id, CancellationToken ct) =>
        Task.FromResult(Documents.GetValueOrDefault(id));

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
