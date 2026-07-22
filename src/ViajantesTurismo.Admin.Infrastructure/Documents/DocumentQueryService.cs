using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Provides no-tracking Admin document read projections.</summary>
internal sealed class DocumentQueryService(AdminReadDbContext dbContext) : IDocumentQueryService
{
    public async Task<GetDocumentDto?> GetById(Guid id, CancellationToken ct)
    {
        var document = await dbContext.DocumentDrafts
            .Include(document => document.Fields.OrderBy(field => field.SortOrder))
            .SingleOrDefaultAsync(document => document.Id == id, ct);

        return document is null ? null : DocumentMapper.MapToGetDocumentDto(document);
    }

    public Task<DocumentAuditMetadata?> GetAuditMetadataById(Guid id, CancellationToken ct) =>
        dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(document => document.Id == id)
            .Select(document => new DocumentAuditMetadata(document.BookingId, document.Revision))
            .SingleOrDefaultAsync(ct);
}
