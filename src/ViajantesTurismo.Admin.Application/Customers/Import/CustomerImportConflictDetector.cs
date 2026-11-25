using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using SharedKernel.InputNormalization;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Detects customer-import conflicts from CSV content and existing persisted customers.
/// </summary>
public sealed class CustomerImportConflictDetector(ICustomerStore customerStore)
{
    /// <summary>
    /// Finds import conflicts caused by duplicate rows or existing customers.
    /// </summary>
    /// <param name="csvText">CSV content to inspect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unique conflicts keyed by email.</returns>
    public async Task<IReadOnlyList<ImportConflictDto>> FindDatabaseEmailConflicts(string csvText, CancellationToken ct)
    {
        var documentResult = CsvDocument.Parse(csvText);
        return documentResult.IsFailure ? [] : await FindDatabaseEmailConflicts(documentResult.Value, ct);
    }

    /// <summary>
    /// Finds import conflicts in a document that was already parsed.
    /// </summary>
    /// <param name="document">Parsed CSV document to inspect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unique conflicts keyed by email.</returns>
    public async Task<IReadOnlyList<ImportConflictDto>> FindDatabaseEmailConflicts(CsvDocument document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);

        var conflictLineNumbers = new HashSet<int>(
            DuplicateDetector.FindDuplicateEmailLineNumbers(document));

        conflictLineNumbers.UnionWith(DuplicateDetector.FindDuplicateNameLineNumbers(document));
        conflictLineNumbers.UnionWith(await FindDuplicateEmailLineNumbersAgainstDatabase(document, ct));

        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conflicts = new List<ImportConflictDto>();

        foreach (var lineNumber in conflictLineNumbers.Order())
        {
            var rowIndex = lineNumber - 2;
            var row = document.Rows[rowIndex];
            if (!row.TryGetByHeader(document.Headers, ImportHeaders.Email, out var email) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var normalizedEmail = email.Trim();
            if (!seenEmails.Add(normalizedEmail))
            {
                continue;
            }

            conflicts.Add(new ImportConflictDto(normalizedEmail));
        }

        return conflicts;
    }

    /// <summary>Finds the existing customer IDs for the supplied import conflict keys in input order.</summary>
    /// <param name="emails">The conflict email keys.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>One ID per input, or null for a conflict without an existing customer.</returns>
    public async Task<Guid?[]> FindExistingCustomerIds(IReadOnlyList<string> emails, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(emails);

        var matches = new Guid?[emails.Count];
        for (var index = 0; index < emails.Count; index++)
        {
            if (StringSanitizer.TrySanitize(emails[index], out var email) && email is not null)
            {
                var customer = await customerStore.GetByEmail(email, ct);
                matches[index] = customer?.Id;
            }
        }

        return matches;
    }

    private async Task<IReadOnlyList<int>> FindDuplicateEmailLineNumbersAgainstDatabase(
        CsvDocument document,
        CancellationToken ct)
    {
        var duplicateLineNumbers = new List<int>();

        foreach (var (row, lineNumber) in document.Rows.Select((row, index) => (row, index + 2)))
        {
            if (!row.TryGetByHeader(document.Headers, ImportHeaders.Email, out var email) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            if (StringSanitizer.TrySanitize(email, out var sanitizedEmail) && sanitizedEmail is not null && await customerStore.EmailExists(sanitizedEmail, ct))
            {
                duplicateLineNumbers.Add(lineNumber);
            }
        }

        return duplicateLineNumbers;
    }
}
