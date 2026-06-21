using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.ApiService;

internal static class ErrorDocumentationEndpoints
{
    public static void MapErrorDocumentationEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/docs/errors");

        group.MapGet("/", GetAllErrorDocumentation)
            .WithName("GetErrorDocumentation")
            .WithDescription("Retrieves generated documentation metadata for centralized error providers.")
            .WithSummary("Retrieves generated error documentation metadata.");

        group.MapGet("/{identifier}", GetErrorDocumentationByIdentifier)
            .WithName("GetErrorDocumentationByIdentifier")
            .WithDescription("Retrieves one generated centralized error documentation entry by identifier.")
            .WithSummary("Retrieves one generated error documentation entry.");
    }

    private static Ok<IReadOnlyList<GetErrorDocumentationDto>> GetAllErrorDocumentation()
    {
        return TypedResults.Ok(ErrorDocumentationCatalog.GetEntries());
    }

    private static Results<Ok<GetErrorDocumentationDto>, NotFound<ProblemDetails>> GetErrorDocumentationByIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Error Documentation Not Found",
                Detail = "Error documentation entry identifier was not provided.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var entry = ErrorDocumentationCatalog.GetEntries().FirstOrDefault(
            candidate => string.Equals(candidate.Identifier, identifier, StringComparison.Ordinal));

        return entry is null
            ? TypedResults.NotFound(new ProblemDetails
            {
                Title = "Error Documentation Not Found",
                Detail = $"Error documentation entry '{identifier}' was not found.",
                Status = StatusCodes.Status404NotFound,
            })
            : TypedResults.Ok(entry);
    }
}
