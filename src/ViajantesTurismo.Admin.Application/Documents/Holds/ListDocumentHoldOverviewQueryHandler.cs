using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Lists holds across document lineages for the hold review overview, one bounded page at a time.</summary>
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
        var search = new DocumentHoldSearch(query.State, query.Basis, query.ReleasedSince, now);
        var skip = (query.Page - DocumentHoldOverviewPaging.FirstPage) * query.PageSize;
        var holds = await holdQuery.Search(search, skip, query.PageSize + 1, ct);

        var items = new List<GetDocumentHoldOverviewItemDto>(query.PageSize);
        foreach (var hold in holds.Take(query.PageSize))
        {
            if (DocumentHoldScopes.TryGetLineageId(hold.Scope, out var lineageId))
            {
                items.Add(new GetDocumentHoldOverviewItemDto
                {
                    LineageId = lineageId,
                    Hold = DocumentHoldMapper.MapToGetDocumentHoldDto(hold, now),
                });
            }
        }

        return Result.Ok(new GetDocumentHoldOverviewPageDto
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            HasMore = holds.Count > query.PageSize,
        });
    }
}
