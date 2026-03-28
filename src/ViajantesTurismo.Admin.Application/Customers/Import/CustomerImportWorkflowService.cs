using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Coordinates customer import workflows, including conflict detection and commit with resolutions.
/// </summary>
public sealed class CustomerImportWorkflowService(
    ICustomerStore customerStore,
    CustomerImportCommandHandler commandHandler
)
{
    private const string EmailFieldName = "Email";
    private const string OutcomeCreated = "created";
    private const string OutcomeUpdated = "updated";

    /// <summary>
    /// Imports customers from CSV, returning conflicts when duplicate emails already exist in the database.
    /// </summary>
    /// <param name="csvText">CSV content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import summary with optional conflicts.</returns>
    public async Task<ImportResultDto> Import(string csvText, CancellationToken ct)
    {
        var conflicts = await FindDatabaseEmailConflicts(csvText, ct);
        if (conflicts.Count > 0)
        {
            return new ImportResultDto(0, 0, conflicts);
        }

        var summary = await AnalyzeRowsForSummary(csvText, conflictResolutions: null, ct);

        var result = await commandHandler.Handle(new CustomerImportCommand(csvText, false), ct);
        var successRows = await BuildSuccessRows(summary.SuccessCandidates, ct);

        return new ImportResultDto(
            result.SuccessCount,
            result.ErrorCount,
            null,
            successRows,
            summary.ErrorRows);
    }

    /// <summary>
    /// Commits customer import by applying user-provided conflict resolutions.
    /// </summary>
    /// <param name="csvText">CSV content.</param>
    /// <param name="conflictResolutions">Conflict resolutions keyed by email.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import summary.</returns>
    public async Task<ImportResultDto> Commit(
        string csvText,
        IReadOnlyDictionary<string, string> conflictResolutions,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conflictResolutions);

        var summary = await AnalyzeRowsForSummary(csvText, conflictResolutions, ct);

        var result = await commandHandler.Handle(
            new CustomerImportCommand(csvText, false),
            ct,
            conflictResolutions);

        var successRows = await BuildSuccessRows(summary.SuccessCandidates, ct);

        return new ImportResultDto(
            result.SuccessCount,
            result.ErrorCount,
            null,
            successRows,
            summary.ErrorRows);
    }

    private async Task<IReadOnlyList<ImportSuccessRowDto>> BuildSuccessRows(
        IReadOnlyList<ImportSuccessCandidate> candidates,
        CancellationToken ct)
    {
        var rows = new List<ImportSuccessRowDto>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var customer = await customerStore.GetByEmail(candidate.Email, ct);
            if (customer is null)
            {
                continue;
            }

            rows.Add(new ImportSuccessRowDto(candidate.Email, candidate.Outcome, customer.Id));
        }

        return rows;
    }

    private async Task<ImportSummaryAnalysis> AnalyzeRowsForSummary(
        string csvText,
        IReadOnlyDictionary<string, string>? conflictResolutions,
        CancellationToken ct)
    {
        var documentResult = CsvDocument.Parse(csvText);
        if (documentResult.IsFailure)
        {
            return new ImportSummaryAnalysis(
                [],
                [new ImportErrorRowDto(1, "Csv", documentResult.ErrorDetails?.Detail ?? "Invalid CSV content")]);
        }

        var document = documentResult.Value;
        var duplicateEmailLines = new HashSet<int>(DuplicateDetector.FindDuplicateEmailLineNumbers(document));

        var successCandidates = new List<ImportSuccessCandidate>();
        var errorRows = new List<ImportErrorRowDto>();

        for (var rowIndex = 0; rowIndex < document.Rows.Count; rowIndex++)
        {
            var lineNumber = rowIndex + 2;
            var row = document.Rows[rowIndex];

            row.TryGetByHeader(document.Headers, EmailFieldName, out var rowEmail);
            var normalizedEmail = rowEmail?.Trim();

            if (duplicateEmailLines.Contains(lineNumber))
            {
                errorRows.Add(new ImportErrorRowDto(
                    lineNumber,
                    EmailFieldName,
                    "Duplicate email found in file.",
                    normalizedEmail));
                continue;
            }

            var customerResult = RowToCustomerMapper.MapCustomer(document, row, TimeProvider.System);
            if (customerResult.IsFailure)
            {
                var (field, message) = GetValidationFieldAndMessage(customerResult);
                errorRows.Add(new ImportErrorRowDto(lineNumber, field, message, normalizedEmail));
                continue;
            }

            var email = customerResult.Value.ContactInfo.Email;
            var emailExists = await customerStore.EmailExists(email, ct);

            if (emailExists)
            {
                var decision = ResolveDecision(conflictResolutions, email);
                switch (decision)
                {
                    case ConflictDecision.Skip:
                        continue;
                    case ConflictDecision.Update:
                        successCandidates.Add(new ImportSuccessCandidate(email, OutcomeUpdated));
                        continue;
                    default:
                        errorRows.Add(new ImportErrorRowDto(
                            lineNumber,
                            EmailFieldName,
                            "Conflict requires resolution.",
                            email));
                        continue;
                }
            }

            successCandidates.Add(new ImportSuccessCandidate(email, OutcomeCreated));
        }

        return new ImportSummaryAnalysis(successCandidates, errorRows);
    }

    private static (string? Field, string Message) GetValidationFieldAndMessage(Result<Customer> result)
    {
        var details = result.ErrorDetails;
        if (details?.ValidationErrors is { Count: > 0 } validationErrors)
        {
            var first = validationErrors.First();
            var message = first.Value.FirstOrDefault() ?? details.Detail;
            return (first.Key, message);
        }

        return (null, details?.Detail ?? "Row failed validation.");
    }

    private static ConflictDecision ResolveDecision(
        IReadOnlyDictionary<string, string>? conflictResolutions,
        string email)
    {
        if (conflictResolutions is null)
        {
            return ConflictDecision.Unresolved;
        }

        var match = conflictResolutions.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, email, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(match.Key))
        {
            return ConflictDecision.Unresolved;
        }

        var shouldUpdate = match.Value.Equals("overwrite", StringComparison.OrdinalIgnoreCase)
            || match.Value.Equals("mixed", StringComparison.OrdinalIgnoreCase);

        return shouldUpdate
            ? ConflictDecision.Update
            : ConflictDecision.Skip;
    }

    private sealed record ImportSuccessCandidate(string Email, string Outcome);

    private sealed record ImportSummaryAnalysis(
        IReadOnlyList<ImportSuccessCandidate> SuccessCandidates,
        IReadOnlyList<ImportErrorRowDto> ErrorRows);

    private enum ConflictDecision
    {
        Unresolved,
        Skip,
        Update,
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
        var conflictLineNumbers = new HashSet<int>(
            DuplicateDetector.FindDuplicateEmailLineNumbers(document));

        conflictLineNumbers.UnionWith(DuplicateDetector.FindDuplicateNameLineNumbers(document));
        conflictLineNumbers.UnionWith(await DuplicateDetector.FindDuplicateEmailLineNumbersAgainstDatabase(
            document,
            customerStore,
            ct));

        if (conflictLineNumbers.Count == 0)
        {
            return [];
        }

        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conflicts = new List<ImportConflictDto>();

        foreach (var lineNumber in conflictLineNumbers.Order())
        {
            var rowIndex = lineNumber - 2;
            if (rowIndex < 0 || rowIndex >= document.Rows.Count)
            {
                continue;
            }

            var row = document.Rows[rowIndex];
            if (!row.TryGetByHeader(document.Headers, EmailFieldName, out var email) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var normalized = email.Trim();
            if (!seenEmails.Add(normalized))
            {
                continue;
            }

            conflicts.Add(new ImportConflictDto(normalized));
        }

        return conflicts;
    }
}
