using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Represents a parsed CSV document with required headers and data rows.
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
    /// CSV data rows (excluding the header row).
    /// </summary>
    public IReadOnlyList<CsvRow> Rows { get; }

    /// <summary>
    /// Parses CSV content into a document with headers and rows.
    /// </summary>
    /// <param name="csvContent">Full CSV content, where the first line is the header row.</param>
    /// <returns>A parsed CSV document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="csvContent"/> is null.</exception>
    public static Result<CsvDocument> Parse(string csvContent)
    {
        ArgumentNullException.ThrowIfNull(csvContent);

        var lines = csvContent
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
        {
            return CsvErrors.HeadersMustContainAtLeastOneColumn();
        }

        var headerRow = CsvRow.Parse(lines[0]);
        var headers = Enumerable.Range(0, headerRow.Count).Select(i => headerRow[i]).ToArray();

        var rows = lines
            .Skip(1)
            .Select(CsvRow.Parse)
            .ToArray();

        return Create(headers, rows);
    }

    /// <summary>
    /// Creates a <see cref="CsvDocument"/> from explicit headers and data rows.
    /// </summary>
    /// <param name="headers">The CSV header columns.</param>
    /// <param name="rows">The CSV data rows (excluding headers).</param>
    /// <returns>A new instance of CsvDocument containing headers and rows.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="headers"/> or <paramref name="rows"/> is null.
    /// </exception>
    public static Result<CsvDocument> Create(IReadOnlyList<string> headers, IReadOnlyList<CsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        if (headers.Count == 0)
        {
            return CsvErrors.HeadersMustContainAtLeastOneColumn();
        }

        var rowsHaveInconsistentColumnCounts = rows.Select(row => row.Count).Distinct().Count() > 1;
        if (rowsHaveInconsistentColumnCounts)
        {
            return CsvErrors.RowsHaveInconsistentColumnCounts();
        }

        var headerCountDiffersFromRows = rows.Any(row => row.Count != headers.Count);
        if (headerCountDiffersFromRows)
        {
            return CsvErrors.HeaderCountMustMatchRowColumnCount();
        }

        return new CsvDocument(headers, rows);
    }
}
