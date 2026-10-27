using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

/// <summary>Document store whose lineage lookup fails with the supplied exception.</summary>
/// <param name="exception">The exception returned by the failed lineage lookup.</param>
internal sealed class ThrowingDocumentStore(Exception exception) : IDocumentStore
{
    private readonly Exception exception = exception ?? throw new ArgumentNullException(nameof(exception));

    /// <summary>Accepts the lineage without storing it.</summary>
    public void Add(DocumentLineage lineage)
    {
        ArgumentNullException.ThrowIfNull(lineage);
    }

    /// <summary>Returns a task faulted with the configured exception.</summary>
    public Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromException<DocumentLineage?>(exception);
    }

    /// <summary>Purges nothing and returns zero.</summary>
    public Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(0);
    }
}
