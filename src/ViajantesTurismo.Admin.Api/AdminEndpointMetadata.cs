using SharedKernel.OpenApi;

namespace ViajantesTurismo.Admin.Api;

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

    /// <summary>Describes the canonical resource address returned on creation.</summary>
    public static RouteHandlerBuilder WithCreatedResourceLocation(this RouteHandlerBuilder builder) =>
        builder.WithLocationResponseHeader("URI reference for the created resource, also returned as its self link.");
}
