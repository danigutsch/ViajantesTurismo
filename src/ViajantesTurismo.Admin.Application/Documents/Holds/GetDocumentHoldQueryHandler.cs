using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Reads one hold, released or not, of the lineage that contains a document.</summary>
public sealed class GetDocumentHoldQueryHandler(IDocumentHoldQuery holdQuery, TimeProvider timeProvider)
{
    /// <summary>Returns the hold; an unknown document, an unknown hold and a hold of another lineage are all not-found.</summary>
    public async Task<Result<GetDocumentHoldDto>> Handle(GetDocumentHoldQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var lineageId = await holdQuery.FindLineageId(query.DocumentId, ct);
        if (lineageId is null)
        {
            return DocumentErrors.DocumentNotFound().ConvertError<GetDocumentHoldDto>();
        }

        var hold = await holdQuery.FindForLineage(lineageId.Value, query.HoldId, ct);
        return hold is null
            ? DocumentHoldErrors.HoldNotFound().ConvertError<GetDocumentHoldDto>()
            : Result.Ok(DocumentHoldMapper.MapToGetDocumentHoldDto(hold, timeProvider.GetUtcNow()));
    }
}
