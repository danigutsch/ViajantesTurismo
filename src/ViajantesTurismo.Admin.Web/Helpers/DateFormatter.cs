using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ViajantesTurismo.Admin.Web.Helpers;

/// <summary>
/// Centralizes date formatting so that switching to culture-aware
/// formatting (e.g., via request localization) requires changes in
/// only one place.
/// </summary>
public static class DateFormatter
{
    public const string DateFormat = "dd/MM/yyyy";
    public const string DateTimeFormat = "dd/MM/yyyy HH:mm";
    private static CultureInfo Culture => CultureInfo.InvariantCulture;

    /// <summary>
    /// Formats a nullable date without time component.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? FormatDate(DateTime? value)
        => value?.ToString(DateFormat, Culture);

    /// <summary>
    /// Formats a date with time component.
    /// </summary>
    public static string FormatDateTime(DateTime value)
        => value.ToString(DateTimeFormat, Culture);
}
