using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SharedKernel.HttpCaching.AspNetCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Api.Errors;

/// <summary>Endpoints serving generated error documentation.</summary>
internal static class ErrorDocumentationEndpoints
{
    private static readonly string CatalogVersion = string.Join('.',
        typeof(ErrorDocumentationEndpoints).Module.ModuleVersionId,
        typeof(GetErrorDocumentationDto).Module.ModuleVersionId,
        typeof(IQueryService).Module.ModuleVersionId,
        typeof(TourErrors).Module.ModuleVersionId);

    /// <summary>Maps the error documentation list and lookup endpoints, requiring documentation read permission.</summary>
    public static void MapErrorDocumentationEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var errorDocumentationGroup = app.MapErrorDocumentationGroup()
            .RequireAuthorization(AdminAuthorization.DocumentationRead);

        errorDocumentationGroup.MapGet("/", GetAllErrorDocumentation)
            .WithName("GetErrorDocumentation")
            .Produces(StatusCodes.Status304NotModified)
            .WithDescription("Retrieves generated documentation metadata for centralized error providers.")
            .WithSummary("Retrieves generated error documentation metadata.");

        errorDocumentationGroup.MapGet("/{identifier}", GetErrorDocumentationByIdentifier)
            .WithName("GetErrorDocumentationByIdentifier")
            .Produces(StatusCodes.Status304NotModified)
            .WithDescription("Retrieves one generated centralized error documentation entry by identifier.")
            .WithSummary("Retrieves one generated error documentation entry.");
    }

    private static Results<Ok<IReadOnlyList<GetErrorDocumentationDto>>, StatusCodeHttpResult> GetAllErrorDocumentation(HttpContext context)
    {
        var entries = ErrorDocumentationCatalog.GetEntries();
        return IsNotModified(context, CatalogVersion)
            ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
            : TypedResults.Ok(entries);
    }

    private static Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>, StatusCodeHttpResult> GetErrorDocumentationByIdentifier(
        HttpContext context,
        string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return MissingEntry(context, "Error documentation entry identifier was not provided.");
        }

        var entry = ErrorDocumentationCatalog.GetEntries().FirstOrDefault(
            candidate => string.Equals(candidate.Identifier, identifier, StringComparison.Ordinal));

        if (entry is null)
        {
            return MissingEntry(context, "Error documentation entry was not found.");
        }

        return IsNotModified(context, $"{CatalogVersion}/{Uri.EscapeDataString(entry.Identifier)}")
            ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
            : TypedResults.Ok(entry);
    }

    private static bool IsNotModified(HttpContext context, string version)
    {
        // Both 200 and 304 responses may be stored privately and must be reauthorized on reuse.
        var tag = new EntityTagHeaderValue($"\"{version}\"", isWeak: true);
        context.Response.Headers.CacheControl = "private, no-cache";
        context.Response.GetTypedHeaders().ETag = tag;

        var requestedTags = context.Request.GetTypedHeaders().IfNoneMatch;
        return requestedTags.Any(candidate => candidate == EntityTagHeaderValue.Any || tag.Compare(candidate, useStrongComparison: false));
    }

    private static NotFound<ProblemDetails> MissingEntry(HttpContext context, string detail)
    {
        HttpCacheHeaders.SetNoStore(context);
        return TypedResults.NotFound(new ProblemDetails
        {
            Title = "Error Documentation Not Found",
            Detail = detail,
            Status = StatusCodes.Status404NotFound,
        });
    }
}
