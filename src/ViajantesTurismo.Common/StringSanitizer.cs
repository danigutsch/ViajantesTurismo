using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace ViajantesTurismo.Common;

/// <summary>
/// Provides string sanitization methods for domain inputs.
/// </summary>
public static partial class StringSanitizer
{
    /// <summary>
    /// Sanitizes a string for general use (names, identifiers).
    /// Trims, normalizes whitespace, removes control characters, and normalizes Unicode.
    /// Returns null only if the input is null.
    /// </summary>
    /// <param name="value">The string to sanitize.</param>
    /// <returns>The sanitized string, or null if input is null.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Sanitize(string? value)
    {
        if (value is null)
        {
            return null;
        }

        value = value.Normalize(NormalizationForm.FormC);
        value = RemoveControlCharacters(value);
        value = value.Trim();
        value = NormalizeWhitespace(value);

        return value;
    }

    /// <summary>
    /// Sanitizes a string for notes/comments where newlines and formatting might be intentional.
    /// Only trims and normalizes Unicode.
    /// </summary>
    /// <param name="value">The string to sanitize.</param>
    /// <returns>The sanitized string, or null if input is null.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? SanitizeNotes(string? value)
    {
        if (value is null)
        {
            return null;
        }

        value = value.Normalize(NormalizationForm.FormC);
        value = value.Trim();

        return value;
    }

    /// <summary>
    /// Sanitizes a collection of strings by removing null/empty entries, trimming, and removing duplicates.
    /// </summary>
    /// <param name="values">The collection to sanitize.</param>
    /// <returns>A sanitized array of unique, non-empty strings.</returns>
    public static string[] SanitizeCollection(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        return
        [
            ..values
                .Select(Sanitize)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static string RemoveControlCharacters(string value) => ControlCharacterRegex().Replace(value, string.Empty);

    private static string NormalizeWhitespace(string value) => MultipleWhitespaceRegex().Replace(value, " ");

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.Compiled)]
    private static partial Regex ControlCharacterRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultipleWhitespaceRegex();
}