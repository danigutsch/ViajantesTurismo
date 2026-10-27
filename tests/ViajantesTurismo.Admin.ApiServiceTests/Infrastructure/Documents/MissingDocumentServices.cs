using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

/// <summary>Document store and query service that behave as if no document exists.</summary>
internal sealed class MissingDocumentServices : IDocumentStore, IDocumentQueryService
{
    /// <summary>Accepts the lineage without storing it.</summary>
    public void Add(DocumentLineage lineage)
    {
        ArgumentNullException.ThrowIfNull(lineage);
    }

    /// <summary>Returns no lineage.</summary>
    public Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct) =>
        Task.FromResult<DocumentLineage?>(null);

    /// <summary>Purges nothing and returns zero.</summary>
    public Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct) =>
        Task.FromResult(0);

    /// <summary>Returns no document.</summary>
    public Task<GetDocumentDto?> GetById(Guid id, CancellationToken ct) =>
        Task.FromResult<GetDocumentDto?>(null);

    /// <summary>Returns no audit metadata.</summary>
    public Task<DocumentAuditMetadata?> GetAuditMetadataById(Guid id, CancellationToken ct) =>
        Task.FromResult<DocumentAuditMetadata?>(null);
}
