using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

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
    public static RouteHandlerBuilder WithCreatedResourceLocation(this RouteHandlerBuilder builder)
    {
        return builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            if (operation.Responses?.TryGetValue("201", out var createdResponse) == true
                && createdResponse is OpenApiResponse response)
            {
                response.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal);
                response.Headers[HeaderNames.Location] = new OpenApiHeader
                {
                    Description = "URI reference for the created resource, also returned as its self link.",
                    Required = true,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri-reference" }
                };
            }

            return Task.CompletedTask;
        });
    }
}
