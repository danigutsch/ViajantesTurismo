namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Defines the paging bounds of the cross-document hold overview.</summary>
public static class DocumentHoldOverviewPaging
{
    /// <summary>The first page number.</summary>
    public const int FirstPage = 1;

    /// <summary>The page size used when a request names none.</summary>
    public const int DefaultPageSize = 25;

    /// <summary>The smallest accepted page size.</summary>
    public const int MinPageSize = 1;

    /// <summary>The largest accepted page size.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The largest accepted page number, which keeps the skipped row count far below the integer range.</summary>
    public const int MaxPage = 10_000;
}
