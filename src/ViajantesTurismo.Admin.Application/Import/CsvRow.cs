using PandaBear.BuildingBlocks;
namespace ViajantesTurismo.Admin.Application.Import;

/// <summary>
/// Represents a single row parsed from a CSV file.
/// </summary>
public sealed class CsvRow : ValueObject, IImportRow
{
    internal CsvRow(PandaBear.Tabular.CsvRow row)
    {
        Row = row;
    }

    internal PandaBear.Tabular.CsvRow Row { get; }

    /// <inheritdoc />
    public int Count => Row.Count;

    /// <inheritdoc />
    public string this[int index] => Row[index];

    /// <inheritdoc />
    public string this[IReadOnlyList<string> headers, string headerName] => Row[headers, headerName];

    /// <inheritdoc />
    public bool TryGetByHeader(IReadOnlyList<string> headers, string headerName, out string? value) =>
        Row.TryGetByHeader(headers, headerName, out value);

    /// <summary>
    /// Parses a CSV line into a CsvRow.
    /// </summary>
    /// <param name="csvLine">The CSV line to parse.</param>
    /// <returns>A CsvRow with parsed values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when csvLine is null.</exception>
    public static CsvRow Parse(string csvLine)
    {
        return new CsvRow(PandaBear.Tabular.CsvRow.Parse(csvLine));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Row;
    }
}
