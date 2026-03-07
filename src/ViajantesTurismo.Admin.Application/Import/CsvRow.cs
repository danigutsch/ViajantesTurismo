using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Admin.Application.Import;

/// <summary>
/// Represents a single row parsed from a CSV file.
/// </summary>
public sealed class CsvRow : ValueObject, IImportRow
{
    private readonly IReadOnlyList<string> _values;

    /// <inheritdoc />
    public int Count => _values.Count;

    private CsvRow(IReadOnlyList<string> values)
    {
        _values = values;
    }

    /// <inheritdoc />
    public string this[int index] => _values[index];

    /// <inheritdoc />
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

    /// <inheritdoc />
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
