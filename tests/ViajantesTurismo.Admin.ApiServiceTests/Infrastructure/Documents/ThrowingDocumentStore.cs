using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

internal sealed class ThrowingDocumentStore(Exception exception) : IDocumentStore
{
    private readonly Exception exception = exception ?? throw new ArgumentNullException(nameof(exception));

    public void Add(DocumentLineage lineage)
    {
        ArgumentNullException.ThrowIfNull(lineage);
    }

    public Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromException<DocumentLineage?>(exception);
    }

    public Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }
}
