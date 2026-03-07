namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Command carrying customer import execution options and counters.
/// </summary>
/// <param name="SuccessCount">Number of successfully processed rows.</param>
/// <param name="ErrorCount">Number of rows that failed processing.</param>
/// <param name="DryRun">Whether the import should skip persistence.</param>
public sealed record CustomerImportCommand(int SuccessCount, int ErrorCount, bool DryRun);
