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
    public static CsvDocument Create(IEnumerable<CsvRow> rows)
    {
        return new CsvDocument(rows);
    }
}
