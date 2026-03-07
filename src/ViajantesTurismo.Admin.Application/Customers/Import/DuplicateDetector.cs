using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Detects duplicate entries inside a CSV document.
/// </summary>
public static class DuplicateDetector
{
    /// <summary>
    /// Returns CSV line numbers for rows that duplicate a previously seen email.
    /// </summary>
    /// <param name="document">CSV document containing headers and rows.</param>
    /// <returns>Line numbers (1-based CSV lines) for duplicate email rows.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
    public static IReadOnlyList<int> FindDuplicateEmailLineNumbers(CsvDocument document)
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
}
