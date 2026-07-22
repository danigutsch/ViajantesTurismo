using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

internal sealed class MissingDocumentServices : IDocumentStore, IDocumentQueryService
{
    public void Add(DocumentLineage lineage)
    {
        ArgumentNullException.ThrowIfNull(lineage);
    }

    public Task<DocumentLineage?> GetByDocumentId(Guid documentId, CancellationToken ct) =>
        Task.FromResult<DocumentLineage?>(null);

    public Task<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct) =>
        Task.FromResult(0);

    public Task<GetDocumentDto?> GetById(Guid id, CancellationToken ct) =>
        Task.FromResult<GetDocumentDto?>(null);

    public Task<DocumentAuditMetadata?> GetAuditMetadataById(Guid id, CancellationToken ct) =>
        Task.FromResult<DocumentAuditMetadata?>(null);
}
