namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Command to import customers from raw CSV content.
/// </summary>
/// <param name="CsvContent">Raw CSV text, where the first line is the header row.</param>
/// <param name="DryRun">When true, parsing and mapping are performed but changes are not persisted.</param>
public sealed record CustomerImportCommand(string CsvContent, bool DryRun);
