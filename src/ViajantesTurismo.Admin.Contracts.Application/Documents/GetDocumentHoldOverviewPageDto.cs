namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Represents one bounded page of the cross-document hold overview.</summary>
public sealed record GetDocumentHoldOverviewPageDto
{
    /// <summary>Gets the holds of the page, oldest placement first.</summary>
    public required IReadOnlyList<GetDocumentHoldOverviewItemDto> Items
    {
        get; init;
    }

    /// <summary>Gets the one-based page number.</summary>
    public required int Page
    {
        get; init;
    }

    /// <summary>Gets the maximum number of holds per page.</summary>
    public required int PageSize
    {
        get; init;
    }

    /// <summary>Gets whether at least one more hold follows this page.</summary>
    public required bool HasMore
    {
        get; init;
    }
}
