using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Common;

/// <summary>
/// Constants for <see cref="SuppressMessageAttribute"/> usage.
/// </summary>
[SuppressMessage(CategoryStyle, CheckIdIDE1006, Justification = "Constant names preserve original analyzer rule IDs (e.g. CA1000, S4035) for traceability.")]
internal static class SuppressConstants
{
    internal const string CategoryDesign = "Design";
    internal const string CategoryStyle = "Style";

    internal const string CheckIdCA1000 = "CA1000:Do not declare static members on generic types";
    internal const string CheckIdIDE1006 = "IDE1006:Naming rule violation";
    internal const string CheckIdS4035 = "S4035:Classes implementing IEquatable<T> should be sealed";
}
