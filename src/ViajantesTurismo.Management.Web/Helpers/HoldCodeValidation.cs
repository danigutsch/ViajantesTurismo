using System.Text.RegularExpressions;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>Validates the opaque hold codes against the same pattern and length the Admin API enforces.</summary>
internal static class HoldCodeValidation
{
    private static readonly Regex CodePattern = new(
        ContractConstants.DocumentHoldCodePattern,
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    /// <summary>Returns a user-facing message for an invalid code, or <see langword="null" /> when the code is valid.</summary>
    /// <param name="code">The trimmed code.</param>
    /// <param name="label">The field label used in the message.</param>
    /// <returns>The message, or <see langword="null" />.</returns>
    public static string? Validate(string code, string label)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        if (code.Length == 0)
        {
            return $"{label} is required.";
        }

        if (code.Length > ContractConstants.MaxDocumentHoldCodeLength)
        {
            return $"{label} must be at most {ContractConstants.MaxDocumentHoldCodeLength} characters.";
        }

        return CodePattern.IsMatch(code)
            ? null
            : $"{label} may contain only letters, digits and . _ : -";
    }
}
