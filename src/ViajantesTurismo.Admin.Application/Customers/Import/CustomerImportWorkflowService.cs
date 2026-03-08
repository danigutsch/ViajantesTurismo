using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Coordinates customer import workflows, including conflict detection and commit with resolutions.
/// </summary>
public sealed class CustomerImportWorkflowService(
    ICustomerStore customerStore,
    CustomerImportCommandHandler commandHandler
)
{
    /// <summary>
    /// Imports customers from CSV, returning conflicts when duplicate emails already exist in the database.
    /// </summary>
    /// <param name="csvText">CSV content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import summary with optional conflicts.</returns>
    public async Task<ImportResultDto> ImportAsync(string csvText, CancellationToken ct)
    {
        var conflicts = await FindDatabaseEmailConflicts(csvText, ct);
        if (conflicts.Count > 0)
        {
            return new ImportResultDto(0, 0, conflicts);
        }

        var result = await commandHandler.Handle(new CustomerImportCommand(csvText, false), ct);
        return new ImportResultDto(result.SuccessCount, result.ErrorCount);
    }

    /// <summary>
    /// Commits customer import by applying user-provided conflict resolutions.
    /// </summary>
    /// <param name="csvText">CSV content.</param>
    /// <param name="conflictResolutions">Conflict resolutions keyed by email.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import summary.</returns>
    public async Task<ImportResultDto> CommitAsync(
        string csvText,
        IReadOnlyDictionary<string, string> conflictResolutions,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conflictResolutions);

        var result = await commandHandler.Handle(
            new CustomerImportCommand(csvText, false),
            ct,
            conflictResolutions);

        return new ImportResultDto(result.SuccessCount, result.ErrorCount);
    }

    private async Task<IReadOnlyList<ImportConflictDto>> FindDatabaseEmailConflicts(
        string csvText,
        CancellationToken ct)
    {
        var documentResult = CsvDocument.Parse(csvText);
        if (documentResult.IsFailure)
        {
            return [];
        }

        var document = documentResult.Value;
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conflicts = new List<ImportConflictDto>();

        foreach (var row in document.Rows)
        {
            if (!row.TryGetByHeader(document.Headers, "Email", out var email) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var normalized = email.Trim();
            if (!seenEmails.Add(normalized))
            {
                continue;
            }

            if (await customerStore.EmailExists(normalized, ct))
            {
                conflicts.Add(new ImportConflictDto(normalized));
            }
        }

        return conflicts;
    }
}
