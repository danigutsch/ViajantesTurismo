using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.Web.Helpers;

/// <summary>
/// Helper class for formatting enum values into human-readable labels.
/// </summary>
public static partial class EnumFormatter
{
    /// <summary>
    /// Formats an enum value by inserting spaces before uppercase letters.
    /// For example, <c>SingleRoom</c> becomes <c>Single Room</c>.
    /// </summary>
    public static string Format<T>(T value) where T : struct, Enum
        => PascalCaseBoundary().Replace(value.ToString(), " ");

    [GeneratedRegex(@"(?<=\p{Ll})(?=\p{Lu})")]
    private static partial Regex PascalCaseBoundary();
}
