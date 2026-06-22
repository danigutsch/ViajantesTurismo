using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.AspNet;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.ApiService;

internal static class ErrorDocumentationEndpoints
{
    public static void MapErrorDocumentationEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/docs/errors");

        group.MapGet(AdminEndpoints.ErrorDocumentation.GetAll.Pattern, GetAllErrorDocumentation)
            .WithEndpointMetadata(AdminEndpoints.ErrorDocumentation.GetAll);

        group.MapGet(AdminEndpoints.ErrorDocumentation.GetByIdentifier.Pattern, GetErrorDocumentationByIdentifier)
            .WithEndpointMetadata(AdminEndpoints.ErrorDocumentation.GetByIdentifier);
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
