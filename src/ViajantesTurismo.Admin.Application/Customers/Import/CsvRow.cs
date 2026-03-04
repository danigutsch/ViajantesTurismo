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
    /// Gets a row value by header name using the provided headers.
    /// </summary>
    /// <param name="headers">The header columns aligned with this row.</param>
    /// <param name="headerName">The header name to resolve.</param>
    /// <returns>The row value under the given header.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> or <paramref name="headerName"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the specified header does not exist.</exception>
    public string this[IReadOnlyList<string> headers, string headerName]
    {
        get
        {
            if (!TryGetByHeader(headers, headerName, out var value))
            {
                throw new KeyNotFoundException($"Header '{headerName}' was not found.");
            }

            return value!;
        }
    }

    /// <summary>
    /// Tries to get a row value by header name.
    /// </summary>
    /// <param name="headers">The header columns aligned with this row.</param>
    /// <param name="headerName">The header name to resolve.</param>
    /// <param name="value">The resolved value when found; otherwise null.</param>
    /// <returns>True when the header exists; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> or <paramref name="headerName"/> is null.</exception>
    public bool TryGetByHeader(IReadOnlyList<string> headers, string headerName, out string? value)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(headerName);

        var normalizedHeaderName = headerName.Trim();

        var headerIndex = headers
            .Select((header, index) => new { header, index })
            .FirstOrDefault(item => string.Equals(item.header, normalizedHeaderName, StringComparison.OrdinalIgnoreCase))
            ?.index ?? -1;

        if (headerIndex < 0)
        {
            value = null;
            return false;
        }

        value = this[headerIndex];
        return true;
    }

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
