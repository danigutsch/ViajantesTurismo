using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Lists every hold, released or not, on the lineage that contains a document.</summary>
public sealed class ListDocumentHoldsQueryHandler(IDocumentHoldQuery holdQuery, TimeProvider timeProvider)
{
    /// <summary>Returns the holds oldest first, or not-found when the document does not exist.</summary>
    public async Task<Result<IReadOnlyList<GetDocumentHoldDto>>> Handle(ListDocumentHoldsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var lineageId = await holdQuery.FindLineageId(query.DocumentId, ct);
        if (lineageId is null)
        {
            return DocumentErrors.DocumentNotFound().ConvertError<IReadOnlyList<GetDocumentHoldDto>>();
        }

        var now = timeProvider.GetUtcNow();
        var holds = await holdQuery.ListForLineage(lineageId.Value, ct);
        IReadOnlyList<GetDocumentHoldDto> dtos = [.. holds.Select(hold => DocumentHoldMapper.MapToGetDocumentHoldDto(hold, now))];
        return Result.Ok(dtos);
    }
}
