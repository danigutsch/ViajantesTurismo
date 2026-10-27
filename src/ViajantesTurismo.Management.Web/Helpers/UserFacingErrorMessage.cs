namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>
/// Provides generic error messages that are safe to display to end users.
/// </summary>
internal static class UserFacingErrorMessage
{
    /// <summary>
    /// The fixed message shown next to a field when the server rejects its value without a usable message.
    /// </summary>
    public const string InvalidFieldValue = "This value is not valid. Check it and try again.";

    /// <summary>
    /// Creates a generic error message for a failed user-facing operation.
    /// </summary>
    /// <param name="action">The human-readable action that could not be completed.</param>
    /// <returns>A sanitized error message.</returns>
    public static string ForOperation(string action) => $"We couldn't {action} right now. Please try again.";
}
