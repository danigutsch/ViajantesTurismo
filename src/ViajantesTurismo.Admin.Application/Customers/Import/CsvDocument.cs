using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Represents a parsed CSV document with headers and rows.
/// </summary>
public sealed class CsvDocument
{
    private CsvDocument(IEnumerable<CsvRow> rows)
    {
        Rows = [.. rows];
    }

    /// <summary>
    /// CSV data rows.
    /// </summary>
    public IReadOnlyList<CsvRow> Rows { get; }

    /// <summary>
    /// Factory method to create a CsvDocument from a collection of CsvRow objects.
    /// </summary>
    /// <param name="rows">The collection of CsvRow objects to include in the document.</param>
    /// <returns>A new instance of CsvDocument containing the provided rows.</returns>
    public static Result<CsvDocument> Create(IReadOnlyList<CsvRow> rows)
    {
        var allRowsHaveSameLength = rows.Select(row => row.Count).Distinct().Count() == 1;
        if (!allRowsHaveSameLength)
        {
            return CsvErrors.RowsHaveInconsistentColumnCounts();
        }

        return new CsvDocument(rows);
    }
}
