using PandaBear.HttpClients;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>Maps HTTP outcomes to user-facing messages.</summary>
internal static class HttpOutcomeUserMessage
{
    /// <summary>Returns the not-found message for a not-found outcome when provided; otherwise the fallback.</summary>
    public static string For(
        HttpOutcomeKind kind,
        string fallback,
        string? notFound = null)
    {
        return kind switch
        {
            HttpOutcomeKind.NotFound => notFound ?? fallback,
            _ => fallback
        };
    }
}
