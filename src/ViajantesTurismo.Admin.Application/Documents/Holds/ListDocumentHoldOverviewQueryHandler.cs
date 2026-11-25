using SharedKernel.DispositionHold;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Lists a bounded page of holds across document lineages or for the lineage of a selected document.</summary>
public sealed class ListDocumentHoldOverviewQueryHandler(IDocumentHoldQuery holdQuery, TimeProvider timeProvider)
{
    /// <summary>Returns the requested page, or a validation failure for paging outside the bounds.</summary>
    public async Task<Result<GetDocumentHoldOverviewPageDto>> Handle(ListDocumentHoldOverviewQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page is < DocumentHoldOverviewPaging.FirstPage or > DocumentHoldOverviewPaging.MaxPage)
        {
            return DocumentHoldErrors.InvalidField("page").ConvertError<GetDocumentHoldOverviewPageDto>();
        }

        if (query.PageSize is < DocumentHoldOverviewPaging.MinPageSize or > DocumentHoldOverviewPaging.MaxPageSize)
        {
            return DocumentHoldErrors.InvalidField("pageSize").ConvertError<GetDocumentHoldOverviewPageDto>();
        }

        var now = timeProvider.GetUtcNow();
        DispositionScope? scope = null;
        bool? isLineageRestrained = null;
        if (query.DocumentId is { } selectedDocumentId)
        {
            var lineageId = await holdQuery.FindLineageId(selectedDocumentId, ct);
            if (lineageId is null)
            {
                return DocumentErrors.DocumentNotFound().ConvertError<GetDocumentHoldOverviewPageDto>();
            }

            scope = DocumentHoldScopes.ForLineage(lineageId.Value);
            var restraining = new DocumentHoldSearch(null, null, null, now) { Scope = scope, RestrainingOnly = true };
            isLineageRestrained = (await holdQuery.Search(restraining, 0, 1, ct)).Count != 0;
        }

        var search = new DocumentHoldSearch(query.State, query.Basis, query.ReleasedSince, now) { Scope = scope };
        var skip = (query.Page - DocumentHoldOverviewPaging.FirstPage) * query.PageSize;
        var holds = await holdQuery.Search(search, skip, query.PageSize + 1, ct);

        var pageHolds = new List<(Guid LineageId, Hold Hold)>(query.PageSize);
        foreach (var hold in holds.Take(query.PageSize))
        {
            if (DocumentHoldScopes.TryGetLineageId(hold.Scope, out var lineageId))
            {
                pageHolds.Add((lineageId, hold));
            }
        }

        var currentDocuments = await holdQuery.FindCurrentDocumentIds([.. pageHolds.Select(entry => entry.LineageId).Distinct()], ct);
        var items = new List<GetDocumentHoldOverviewItemDto>(pageHolds.Count);
        foreach (var (lineageId, hold) in pageHolds)
        {
            items.Add(new GetDocumentHoldOverviewItemDto
            {
                LineageId = lineageId,
                DocumentId = currentDocuments.TryGetValue(lineageId, out var documentId) ? documentId : null,
                Hold = DocumentHoldMapper.MapToGetDocumentHoldDto(hold, now),
            });
        }

        return Result.Ok(new GetDocumentHoldOverviewPageDto
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            HasMore = holds.Count > query.PageSize,
            IsLineageRestrained = isLineageRestrained,
        });
    }
}
