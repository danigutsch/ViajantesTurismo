using System.Text.RegularExpressions;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>
/// Helper class for formatting enum values into human-readable labels.
/// </summary>
internal static partial class EnumFormatter
{
    /// <summary>
    /// Formats an enum value by inserting spaces before uppercase letters.
    /// For example, <c>DoubleBed</c> becomes <c>Double Bed</c>.
    /// </summary>
    public static string Format<T>(T value) where T : struct, Enum
        => PascalCaseBoundary().Replace(value.ToString(), " ");

    /// <summary>
    /// Formats a <see cref="BikeTypeDto"/> value into a human-readable label.
    /// </summary>
    /// <param name="bikeType">The bike type to format.</param>
    /// <returns>The human-readable bike type label.</returns>
    public static string Format(BikeTypeDto bikeType) => bikeType switch
    {
        BikeTypeDto.None => "None",
        BikeTypeDto.Regular => "Regular Bike",
        BikeTypeDto.EBike => "E-Bike",
        _ => bikeType.ToString()
    };

    [GeneratedRegex(@"(?<=\p{Ll})(?=\p{Lu})")]
    private static partial Regex PascalCaseBoundary();
}
