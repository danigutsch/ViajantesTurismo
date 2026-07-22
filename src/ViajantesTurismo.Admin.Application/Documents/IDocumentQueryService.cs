using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Provides Admin-safe read projections for generated documents.</summary>
public interface IDocumentQueryService
{
    /// <summary>Gets one generated document revision by identifier.</summary>
    Task<GetDocumentDto?> GetById(Guid id, CancellationToken ct);

    /// <summary>Gets the minimal metadata required to record a document audit entry.</summary>
    Task<DocumentAuditMetadata?> GetAuditMetadataById(Guid id, CancellationToken ct);
}
