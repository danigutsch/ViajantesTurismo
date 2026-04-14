using ViajantesTurismo.Admin.Application.Import;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Detects customer-import conflicts from CSV content and existing persisted customers.
/// </summary>
public sealed class CustomerImportConflictDetector(ICustomerStore customerStore)
{
    private const string EmailFieldName = "Email";

    /// <summary>
    /// Finds import conflicts caused by duplicate rows or existing customers.
    /// </summary>
    /// <param name="csvText">CSV content to inspect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unique conflicts keyed by email.</returns>
    public async Task<IReadOnlyList<ImportConflictDto>> FindDatabaseEmailConflicts(string csvText, CancellationToken ct)
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
        conflictLineNumbers.UnionWith(await FindDuplicateEmailLineNumbersAgainstDatabase(document, ct));

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

            var normalizedEmail = email.Trim();
            if (!seenEmails.Add(normalizedEmail))
            {
                continue;
            }

            conflicts.Add(new ImportConflictDto(normalizedEmail));
        }

        return conflicts;
    }

    private async Task<IReadOnlyList<int>> FindDuplicateEmailLineNumbersAgainstDatabase(
        CsvDocument document,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);

        var duplicateLineNumbers = new List<int>();

        foreach (var (row, lineNumber) in document.Rows.Select((row, index) => (row, index + 2)))
        {
            if (!row.TryGetByHeader(document.Headers, EmailFieldName, out var email) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var sanitizedEmail = StringSanitizer.Sanitize(email);
            if (await customerStore.EmailExists(sanitizedEmail, ct))
            {
                duplicateLineNumbers.Add(lineNumber);
            }
        }

        return duplicateLineNumbers;
    }
}
