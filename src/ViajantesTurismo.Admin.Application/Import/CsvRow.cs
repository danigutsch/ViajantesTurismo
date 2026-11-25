using SharedKernel.BuildingBlocks;
namespace ViajantesTurismo.Admin.Application.Import;

/// <summary>
/// Represents a single row parsed from a CSV file.
/// </summary>
public sealed class CsvRow : ValueObject, IImportRow
{
    /// <summary>Wraps a parsed tabular CSV row.</summary>
    internal CsvRow(SharedKernel.Tabular.CsvRow row)
    {
        Row = row;
    }

    /// <summary>Underlying tabular CSV row.</summary>
    internal SharedKernel.Tabular.CsvRow Row
    {
        get;
    }

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
    /// Creates a row from field values, kept exactly as given.
    /// </summary>
    /// <param name="values">The field values.</param>
    /// <returns>A CsvRow holding the values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when values or one of its values is null.</exception>
    public static CsvRow Create(IEnumerable<string> values)
    {
        return new CsvRow(SharedKernel.Tabular.CsvRow.Create(values));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Row;
    }
}
