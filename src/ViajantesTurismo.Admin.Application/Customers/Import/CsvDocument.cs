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
    /// <param name="requiredHeaderNames">Optional list of required headers that must exist in the CSV header row.</param>
    /// <returns>A parsed CSV document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="csvContent"/> is null.</exception>
    public static Result<CsvDocument> Parse(string csvContent, IReadOnlyList<string>? requiredHeaderNames = null)
    {
        ArgumentNullException.ThrowIfNull(csvContent);

        var lines = csvContent
            .Split(["\r\n", "\n"], StringSplitOptions.TrimEntries);

        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            return CsvErrors.HeadersMustContainAtLeastOneColumn();
        }

        var headerRow = CsvRow.Parse(lines[0]);
        var headers = Enumerable.Range(0, headerRow.Count).Select(i => headerRow[i]).ToArray();

        var missingRequiredHeader = requiredHeaderNames?
            .Select(requiredHeader => requiredHeader.Trim())
            .Where(requiredHeader => !string.IsNullOrWhiteSpace(requiredHeader))
            .FirstOrDefault(requiredHeader =>
                !headers.Any(header =>
                    string.Equals(header, requiredHeader, StringComparison.OrdinalIgnoreCase)
                )
            );

        if (missingRequiredHeader is not null)
        {
            return CsvErrors.RequiredHeaderMissing(missingRequiredHeader);
        }

        var rows = lines
            .Skip(1)
            .Select(CsvRow.Parse);

        return Create(headers, [.. rows]);
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

        var firstRowWithInvalidColumnCount = rows
            .Select((row, index) => new { row, index })
            .FirstOrDefault(item => item.row.Count != headers.Count);

        var rowsHaveInconsistentColumnCounts = rows.Select(row => row.Count).Distinct().Count() > 1;
        if (rowsHaveInconsistentColumnCounts)
        {
            if (firstRowWithInvalidColumnCount is not null)
            {
                var csvLineNumber = firstRowWithInvalidColumnCount.index + 2;
                return CsvErrors.RowsHaveInconsistentColumnCounts(csvLineNumber);
            }

            return CsvErrors.RowsHaveInconsistentColumnCounts();
        }

        var headerCountDiffersFromRows = firstRowWithInvalidColumnCount is not null;
        if (headerCountDiffersFromRows)
        {
            var csvLineNumber = firstRowWithInvalidColumnCount!.index + 2;
            return CsvErrors.HeaderCountMustMatchRowColumnCount(csvLineNumber);
        }

        return new CsvDocument(headers, rows);
    }
}
