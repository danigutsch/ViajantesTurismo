namespace ViajantesTurismo.Admin.ApiService;

internal static class AdminEndpointMetadata
{
    public static RouteHandlerBuilder WithAdminMetadata(
        this RouteHandlerBuilder builder,
        string name,
        string description,
        string summary)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .WithName(name)
            .WithDescription(description)
            .WithSummary(summary);
    }
}
