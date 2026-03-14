namespace ViajantesTurismo.Admin.Web.Helpers;

/// <summary>
/// Provides generic error messages that are safe to display to end users.
/// </summary>
internal static class UserFacingErrorMessage
{
    /// <summary>
    /// Creates a generic error message for a failed user-facing operation.
    /// </summary>
    /// <param name="action">The human-readable action that could not be completed.</param>
    /// <returns>A sanitized error message.</returns>
    public static string ForOperation(string action) => $"We couldn't {action} right now. Please try again.";
}
