using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>Maps HTTP outcomes to user-facing messages.</summary>
internal static class HttpOutcomeUserMessage
{
    /// <summary>
    /// Returns the not-found or validation-problem message for those outcomes when provided; otherwise the fallback.
    /// Server validation text is never used, because it is remote-influenced.
    /// </summary>
    public static string For(
        HttpOutcomeKind kind,
        string fallback,
        string? notFound = null,
        string? validationProblem = null)
    {
        return kind switch
        {
            HttpOutcomeKind.NotFound => notFound ?? fallback,
            HttpOutcomeKind.ValidationProblem => validationProblem ?? fallback,
            _ => fallback
        };
    }
}
