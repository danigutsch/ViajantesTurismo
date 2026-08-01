using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.Web.Helpers;

internal static class HttpOutcomeUserMessage
{
    public static string For(
        ContractCommandOutcomeKind kind,
        string fallback,
        string? notFound = null)
    {
        return kind switch
        {
            ContractCommandOutcomeKind.NotFound => notFound ?? fallback,
            _ => fallback
        };
    }
}
