using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Represents a parsed CSV document with headers and rows.
/// </summary>
public sealed class CsvDocument
{
    private CsvDocument(IEnumerable<string> headers, IEnumerable<CsvRow> rows)
    {
        Headers = [.. headers];
        Rows = [.. rows];
    }

    /// <summary>
    /// CSV header columns.
    /// </summary>
    public IReadOnlyList<string> Headers { get; }

    /// <summary>
    /// CSV data rows.
    /// </summary>
    public IReadOnlyList<CsvRow> Rows { get; }

    /// <summary>
    /// Parses rows into a CsvDocument without headers.
    /// </summary>
    /// <param name="rows">The collection of CsvRow objects to include in the document.</param>
    /// <returns>A new instance of CsvDocument containing the provided rows.</returns>
    public static Result<CsvDocument> Parse(IReadOnlyList<CsvRow> rows)
    {
        var allRowsHaveSameLength = rows.Select(row => row.Count).Distinct().Count() == 1;
        if (!allRowsHaveSameLength)
        {
            return CsvErrors.RowsHaveInconsistentColumnCounts();
        }

        return new CsvDocument([], rows);
    }

    /// <summary>
    /// Parses headers and rows into a CsvDocument.
    /// </summary>
    /// <param name="headers">The CSV header columns.</param>
    /// <param name="rows">The collection of CsvRow objects to include in the document.</param>
    /// <returns>A new instance of CsvDocument containing headers and rows.</returns>
    public static Result<CsvDocument> Parse(IReadOnlyList<string> headers, IReadOnlyList<CsvRow> rows)
    {
        if (headers.Count == 0)
        {
            return CsvErrors.HeadersMustContainAtLeastOneColumn();
        }

        var allRowsHaveSameLength = rows.Select(row => row.Count).Distinct().Count() == 1;
        if (!allRowsHaveSameLength)
        {
            return CsvErrors.RowsHaveInconsistentColumnCounts();
        }

        var allRowsMatchHeaderCount = rows.All(row => row.Count == headers.Count);
        if (!allRowsMatchHeaderCount)
        {
            return CsvErrors.HeaderCountMustMatchRowColumnCount();
        }

        return new CsvDocument(headers, rows);
    }

    /// <summary>
    /// Factory method to create a CsvDocument from a collection of CsvRow objects.
    /// </summary>
    /// <param name="rows">The collection of CsvRow objects to include in the document.</param>
    /// <returns>A new instance of CsvDocument containing the provided rows.</returns>
    public static Result<CsvDocument> Create(IReadOnlyList<CsvRow> rows) => Parse(rows);

    /// <summary>
    /// Factory method to create a CsvDocument from headers and rows.
    /// </summary>
    /// <param name="headers">The CSV header columns.</param>
    /// <param name="rows">The collection of CsvRow objects to include in the document.</param>
    /// <returns>A new instance of CsvDocument containing headers and rows.</returns>
    public static Result<CsvDocument> Create(IReadOnlyList<string> headers, IReadOnlyList<CsvRow> rows) => Parse(headers, rows);
}
