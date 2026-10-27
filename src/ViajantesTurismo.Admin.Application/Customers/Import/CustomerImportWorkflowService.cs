using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Coordinates customer import workflows, including conflict detection and commit with resolutions.
/// </summary>
public sealed class CustomerImportWorkflowService(
    ICustomerStore customerStore,
    CustomerImportConflictDetector conflictDetector,
    CustomerImportCommandHandler commandHandler,
    TimeProvider timeProvider
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
    public Task<ImportResultDto> Import(string csvText, CancellationToken ct) =>
        Import(CsvDocument.Parse(csvText), ct);

    /// <summary>
    /// Imports customers from UTF-8 CSV bytes, parsing the content once.
    /// </summary>
    /// <param name="utf8Csv">UTF-8 encoded CSV content without a byte order mark.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import summary with optional conflicts.</returns>
    public Task<ImportResultDto> Import(ReadOnlyMemory<byte> utf8Csv, CancellationToken ct) =>
        Import(CsvDocument.ParseUtf8(utf8Csv.Span), ct);

    private async Task<ImportResultDto> Import(Result<CsvDocument> parsed, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var conflicts = await FindConflicts(parsed, ct);
        if (conflicts.Count > 0)
        {
            return new ImportResultDto(0, 0, conflicts);
        }

        var summary = await AnalyzeRowsForSummary(parsed, conflictResolutions: null, today, ct);

        ImportResult result;
        try
        {
            result = await Execute(parsed, today, conflictResolutions: null, ct);
        }
        catch (CustomerEmailConflictException)
        {
            return await CreatePersistenceConflictResult(parsed, summary.SuccessCandidates, ct);
        }

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
    public Task<ImportResultDto> Commit(
        string csvText,
        IReadOnlyDictionary<string, string> conflictResolutions,
        CancellationToken ct) =>
        Commit(CsvDocument.Parse(csvText), conflictResolutions, ct);

    /// <summary>
    /// Commits customer import from UTF-8 CSV bytes by applying user-provided conflict resolutions, parsing the content once.
    /// </summary>
    /// <param name="utf8Csv">UTF-8 encoded CSV content without a byte order mark.</param>
    /// <param name="conflictResolutions">Conflict resolutions keyed by email.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import summary.</returns>
    public Task<ImportResultDto> Commit(
        ReadOnlyMemory<byte> utf8Csv,
        IReadOnlyDictionary<string, string> conflictResolutions,
        CancellationToken ct) =>
        Commit(CsvDocument.ParseUtf8(utf8Csv.Span), conflictResolutions, ct);

    private async Task<ImportResultDto> Commit(
        Result<CsvDocument> parsed,
        IReadOnlyDictionary<string, string> conflictResolutions,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conflictResolutions);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var summary = await AnalyzeRowsForSummary(parsed, conflictResolutions, today, ct);

        ImportResult result;
        try
        {
            result = await Execute(parsed, today, conflictResolutions, ct);
        }
        catch (CustomerEmailConflictException)
        {
            return await CreatePersistenceConflictResult(parsed, summary.SuccessCandidates, ct);
        }

        var successRows = await BuildSuccessRows(summary.SuccessCandidates, ct);

        return new ImportResultDto(
            result.SuccessCount,
            result.ErrorCount,
            null,
            successRows,
            summary.ErrorRows);
    }

    private Task<IReadOnlyList<ImportConflictDto>> FindConflicts(Result<CsvDocument> parsed, CancellationToken ct) =>
        parsed.IsFailure
            ? Task.FromResult<IReadOnlyList<ImportConflictDto>>([])
            : conflictDetector.FindDatabaseEmailConflicts(parsed.Value, ct);

    private async Task<ImportResult> Execute(
        Result<CsvDocument> parsed,
        DateOnly today,
        IReadOnlyDictionary<string, string>? conflictResolutions,
        CancellationToken ct) =>
        parsed.IsFailure
            ? new ImportResult(0, 0)
            : await commandHandler.Handle(
                new CustomerImportCommand(string.Empty, false) { Today = today, Document = parsed.Value },
                ct,
                conflictResolutions);

    private async Task<ImportResultDto> CreatePersistenceConflictResult(
        Result<CsvDocument> parsed,
        IReadOnlyList<ImportSuccessCandidate> successCandidates,
        CancellationToken ct)
    {
        var conflicts = await FindConflicts(parsed, ct);
        if (conflicts.Count > 0)
        {
            return new ImportResultDto(0, 0, conflicts);
        }

        var persistenceErrors = successCandidates
            .DistinctBy(candidate => candidate.Email, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => new ImportErrorRowDto(
                candidate.LineNumber,
                EmailFieldName,
                "Persistence rejected a duplicate Customer email. Retry the import.",
                candidate.Email))
            .ToArray();
        if (persistenceErrors.Length == 0)
        {
            persistenceErrors =
            [
                new ImportErrorRowDto(
                    1,
                    EmailFieldName,
                    "Persistence rejected a duplicate Customer email. Retry the import.")
            ];
        }

        return new ImportResultDto(0, persistenceErrors.Length, null, null, persistenceErrors);
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
        Result<CsvDocument> documentResult,
        IReadOnlyDictionary<string, string>? conflictResolutions,
        DateOnly today,
        CancellationToken ct)
    {
        if (documentResult.IsFailure)
        {
            return new ImportSummaryAnalysis(
                [],
                [new ImportErrorRowDto(1, "Csv", documentResult.ErrorDetails.Detail)]);
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

            var customerResult = RowToCustomerMapper.MapCustomer(document, row, today);
            if (customerResult.IsFailure)
            {
                var (field, message) = GetValidationFieldAndMessage(customerResult.ErrorDetails);
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
                        successCandidates.Add(new ImportSuccessCandidate(lineNumber, email, OutcomeUpdated));
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

            successCandidates.Add(new ImportSuccessCandidate(lineNumber, email, OutcomeCreated));
        }

        return new ImportSummaryAnalysis(successCandidates, errorRows);
    }

    private static (string? Field, string Message) GetValidationFieldAndMessage(ResultError details)
    {
        if (details.ValidationErrors is { Count: > 0 } validationErrors)
        {
            var first = validationErrors.First();
            return (first.Key, first.Value[0]);
        }

        return (null, details.Detail);
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

    private sealed record ImportSuccessCandidate(int LineNumber, string Email, string Outcome);

    private sealed record ImportSummaryAnalysis(
        IReadOnlyList<ImportSuccessCandidate> SuccessCandidates,
        IReadOnlyList<ImportErrorRowDto> ErrorRows);

    private enum ConflictDecision
    {
        Unresolved,
        Skip,
        Update,
    }
}
