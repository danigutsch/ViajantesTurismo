using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Application.Import;

/// <summary>
/// Detects duplicate entries inside a document.
/// </summary>
public static class DuplicateDetector
{
    /// <summary>
    /// Returns line numbers for rows that duplicate a previously seen email.
    /// </summary>
    /// <param name="document">Document containing headers and rows.</param>
    /// <returns>Line numbers (1-based lines) for duplicate email rows.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
    public static IReadOnlyList<int> FindDuplicateEmailLineNumbers(IImportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var seenEmails = new HashSet<string>(StringComparer.Ordinal);
        var duplicateLineNumbers = new List<int>();

        for (var rowIndex = 0; rowIndex < document.Rows.Count; rowIndex++)
        {
            var row = document.Rows[rowIndex];

            if (!row.TryGetByHeader(document.Headers, "Email", out var email) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var normalizedEmail = StringSanitizer.NormalizeKey(email);
            if (!seenEmails.Add(normalizedEmail))
            {
                duplicateLineNumbers.Add(rowIndex + 2);
            }
        }

        return duplicateLineNumbers;
    }

    /// <summary>
    /// Returns line numbers for rows that duplicate a previously seen name.
    /// </summary>
    /// <param name="document">document containing headers and rows.</param>
    /// <returns>Line numbers (1-based lines) for duplicate name rows.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
    public static IReadOnlyList<int> FindDuplicateNameLineNumbers(IImportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var duplicateLineNumbers = new List<int>();

        for (var rowIndex = 0; rowIndex < document.Rows.Count; rowIndex++)
        {
            var row = document.Rows[rowIndex];

            if (!row.TryGetByHeader(document.Headers, "FirstName", out var firstName)
                || !row.TryGetByHeader(document.Headers, "LastName", out var lastName)
                || string.IsNullOrWhiteSpace(firstName)
                || string.IsNullOrWhiteSpace(lastName))
            {
                continue;
            }

            var normalizedName = StringSanitizer.NormalizeKeyRemovingDiacritics($"{firstName} {lastName}");
            if (!seenNames.Add(normalizedName))
            {
                duplicateLineNumbers.Add(rowIndex + 2);
            }
        }

        return duplicateLineNumbers;
    }

    /// <summary>
    /// Returns line numbers for rows whose email already exists in the database.
    /// </summary>
    /// <param name="document">document containing headers and rows.</param>
    /// <param name="customerStore">Customer store used to check existing emails.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Line numbers (1-based lines) for rows with duplicate emails in the database.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required arguments are null.</exception>
    public static async Task<IReadOnlyList<int>> FindDuplicateEmailLineNumbersAgainstDatabaseAsync(
        IImportDocument document,
        ICustomerStore customerStore,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(customerStore);

        var duplicateLineNumbers = new List<int>();

        for (var rowIndex = 0; rowIndex < document.Rows.Count; rowIndex++)
        {
            var row = document.Rows[rowIndex];

            if (!row.TryGetByHeader(document.Headers, "Email", out var email) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var normalizedEmail = StringSanitizer.NormalizeKey(email);
            if (await customerStore.EmailExists(normalizedEmail, ct))
            {
                duplicateLineNumbers.Add(rowIndex + 2);
            }
        }

        return duplicateLineNumbers;
    }
}
