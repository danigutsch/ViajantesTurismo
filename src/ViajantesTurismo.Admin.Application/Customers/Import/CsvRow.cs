using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Represents a single row parsed from a CSV file.
/// </summary>
public sealed class CsvRow : ValueObject
{
    private readonly IReadOnlyList<string> _values;

    /// <summary>
    /// Gets the number of columns in the row.
    /// </summary>
    public int Count => _values.Count;

    private CsvRow(IReadOnlyList<string> values)
    {
        _values = values;
    }

    /// <summary>
    /// Gets the value at the specified column index.
    /// </summary>
    /// <param name="index">The zero-based column index.</param>
    /// <returns>The value at the index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is out of range.</exception>
    public string this[int index] => _values[index];

    /// <summary>
    /// Parses a CSV line into a CsvRow.
    /// </summary>
    /// <param name="csvLine">The CSV line to parse.</param>
    /// <returns>A CsvRow with parsed values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when csvLine is null.</exception>
    public static CsvRow Parse(string csvLine)
    {
        ArgumentNullException.ThrowIfNull(csvLine);

        var values = csvLine.Split(',').Select(v => v.Trim()).ToList();

        return new CsvRow([.. values]);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return _values;
    }
}
