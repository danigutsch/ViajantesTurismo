using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SharedKernel.AspNet;

/// <summary>
/// Applies required endpoint metadata to Minimal API route handlers.
/// </summary>
public static class EndpointMetadataRouteHandlerBuilderExtensions
{
    /// <summary>
    /// Applies the required endpoint metadata used by generated API documentation.
    /// </summary>
    /// <param name="builder">The route handler builder to configure.</param>
    /// <param name="metadata">The required endpoint metadata.</param>
    /// <returns>The configured route handler builder.</returns>
    public static RouteHandlerBuilder WithEndpointMetadata(
        this RouteHandlerBuilder builder,
        EndpointMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(metadata);

        return builder
            .WithName(metadata.Name)
            .WithSummary(metadata.Summary)
            .WithDescription(metadata.Description);
    }

    /// <summary>
    /// Applies the required endpoint metadata from an endpoint definition.
    /// </summary>
    /// <param name="builder">The route handler builder to configure.</param>
    /// <param name="definition">The endpoint definition containing required metadata.</param>
    /// <returns>The configured route handler builder.</returns>
    public static RouteHandlerBuilder WithEndpointMetadata(
        this RouteHandlerBuilder builder,
        EndpointDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return builder.WithEndpointMetadata(definition.Metadata);
    }
}
