using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Command to import customers from raw CSV content.
/// </summary>
/// <param name="CsvContent">Raw CSV text, where the first line is the header row.</param>
/// <param name="DryRun">When true, parsing and mapping are performed but changes are not persisted.</param>
public sealed record CustomerImportCommand(string CsvContent, bool DryRun)
{
    /// <summary>Gets the already parsed document; when set, the handler uses it instead of parsing <c>CsvContent</c>.</summary>
    public CsvDocument? Document
    {
        get; init;
    }

    /// <summary>Gets the UTC business date captured by the import workflow, when supplied.</summary>
    public DateOnly? Today
    {
        get; init;
    }
}
