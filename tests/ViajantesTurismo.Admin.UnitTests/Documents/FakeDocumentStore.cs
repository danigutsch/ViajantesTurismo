using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class FakeDocumentStore : IDocumentStore
{
    public List<DocumentDraft> AddedDocuments { get; } = [];

    public void Add(DocumentDraft document) => AddedDocuments.Add(document);

    public Task<DocumentDraft?> GetById(Guid id, CancellationToken ct) => Task.FromResult<DocumentDraft?>(null);

    public Task<IReadOnlyList<DocumentDraft>> GetExpiredDrafts(DateTime now, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<DocumentDraft>>([]);

    public void Remove(DocumentDraft document) => AddedDocuments.Remove(document);
}
