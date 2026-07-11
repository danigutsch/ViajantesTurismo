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

    public Task<IReadOnlyList<DocumentDraft>> GetExpiredDrafts(DateTime now, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<DocumentDraft>>(Documents.Values.Where(document => document.IsExpiredDraft(now)).ToArray());

    public void Remove(DocumentDraft document)
    {
        AddedDocuments.Remove(document);
        Documents.Remove(document.Id);
    }
}
