using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Import;

/// <summary>
/// Represents a parsed CSV document with required headers and data rows.
/// </summary>
public sealed class CsvDocument : IImportDocument
{
    private readonly PandaBear.Tabular.CsvDocument document;
    private readonly IReadOnlyList<IImportRow> rows;

    private CsvDocument(PandaBear.Tabular.CsvDocument document)
    {
        this.document = document;
        rows = document.Rows.Select(row => (IImportRow)new CsvRow(row)).ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> Headers => document.Headers;

    /// <inheritdoc/>
    public IReadOnlyList<IImportRow> Rows => rows;

    /// <summary>
    /// Parses CSV content into a document with headers and rows.
    /// </summary>
    /// <param name="content">Full CSV content, where the first line is the header row.</param>
    /// <param name="requiredHeaderNames">Optional list of required headers that must exist in the CSV header row.</param>
    /// <returns>A parsed CSV document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    public static Result<CsvDocument> Parse(string content, IReadOnlyList<string>? requiredHeaderNames = null)
    {
        var parsed = PandaBear.Tabular.CsvDocument.Parse(content, requiredHeaderNames);
        return parsed.IsSuccess
            ? Result.Ok(new CsvDocument(parsed.Value))
            : parsed.ConvertError<PandaBear.Tabular.CsvDocument, CsvDocument>();
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
        ArgumentNullException.ThrowIfNull(rows);

        var created = PandaBear.Tabular.CsvDocument.Create(headers, rows.Select(row => row.Row).ToArray());
        return created.IsSuccess
            ? Result.Ok(new CsvDocument(created.Value))
            : created.ConvertError<PandaBear.Tabular.CsvDocument, CsvDocument>();
    }
}
