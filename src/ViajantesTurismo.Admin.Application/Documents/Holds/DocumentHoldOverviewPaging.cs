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

    /// <summary>
    /// The largest accepted page number. It bounds the offset a single request can force the database to read and
    /// discard: at <see cref="MaxPageSize" /> rows per page the skipped row count stays below 10 million,
    /// far below the 32-bit offset range that <c>Skip</c> accepts.
    /// </summary>
    public const int MaxPage = 100_000;
}
