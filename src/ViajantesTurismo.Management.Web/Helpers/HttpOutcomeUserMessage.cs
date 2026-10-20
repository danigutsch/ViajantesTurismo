using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.Web.Helpers;

internal static class HttpOutcomeUserMessage
{
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
