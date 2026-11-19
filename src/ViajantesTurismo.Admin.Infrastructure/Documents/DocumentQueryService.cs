using Microsoft.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Provides no-tracking Admin document read projections.</summary>
internal sealed class DocumentQueryService(AdminReadDbContext dbContext, IDocumentSourceVersionReader sourceVersionReader) : IDocumentQueryService
{
    /// <summary>Returns the document with fields ordered by sort order, or null when not found.</summary>
    public async Task<GetDocumentDto?> GetById(Guid id, CancellationToken ct)
    {
        var document = await dbContext.DocumentDrafts
            .Include(document => document.Fields.OrderBy(field => field.SortOrder))
            .SingleOrDefaultAsync(document => document.Id == id, ct);

        if (document is null)
        {
            return null;
        }

        if (!document.CanAssessReviewFreshness)
        {
            return DocumentMapper.MapToGetDocumentDto(document);
        }

        var source = await sourceVersionReader.Read(document.BookingId, ct);
        var currentVersion = source.Result.IsSuccess ? Option.Some(source.Result.Value) : Option.None<string>();
        return DocumentMapper.MapToGetDocumentDto(document, currentVersion);
    }

    /// <summary>Returns the booking and revision of the document for auditing, or null when not found.</summary>
    public Task<DocumentAuditMetadata?> GetAuditMetadataById(Guid id, CancellationToken ct) =>
        dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(document => document.Id == id)
            .Select(document => new DocumentAuditMetadata(document.BookingId, document.Revision))
            .SingleOrDefaultAsync(ct);
}
