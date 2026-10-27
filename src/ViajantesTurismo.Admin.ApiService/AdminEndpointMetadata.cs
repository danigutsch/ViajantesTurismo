namespace ViajantesTurismo.Admin.ApiService;

/// <summary>Endpoint metadata helpers for Admin API routes.</summary>
internal static class AdminEndpointMetadata
{
    /// <summary>Sets the endpoint name, description and summary used by OpenAPI.</summary>
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
