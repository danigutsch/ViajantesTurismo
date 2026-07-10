using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.ApiService.Customers;

/// <summary>
/// Defines endpoints for bulk customer import operations.
/// </summary>
internal static class CustomerImportEndpoints
{
    /// <summary>
    /// Maps all customer import endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static WebApplication MapCustomerImportEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var importGroup = app.MapCustomerImportsGroup()
            .RequireRateLimiting(AdminSecurityBaseline.ImportRateLimitPolicy);

        importGroup.MapPost("/", ImportCustomers)
            .WithName("ImportCustomers")
            .WithDescription("Imports customers from a CSV file.")
            .WithSummary("Imports customers from a CSV file.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithMetadata(new RequestSizeLimitAttribute(ContractConstants.CustomerImportMaxRequestBytes))
            .DisableAntiforgery();

        importGroup.MapPost("/commit", CommitImportWithResolutions)
            .WithName("CommitImportWithResolutions")
            .WithDescription("Commits customer import applying conflict resolutions.")
            .WithSummary("Commits customer import applying conflict resolutions.")
            .Accepts<CommitCustomerImportFormDto>("multipart/form-data")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithMetadata(new RequestSizeLimitAttribute(ContractConstants.CustomerImportMaxRequestBytes))
            .DisableAntiforgery();

        return app;
    }

    private static async Task<Results<Ok<ImportResultDto>, ProblemHttpResult>> ImportCustomers(
        [Required]
        IFormFile? file,
        [FromServices] CustomerImportWorkflowService workflow,
        CancellationToken ct)
    {
        if (!TryValidateImportFile(file, out var problem))
        {
            return TypedResults.Problem(
                detail: problem.Detail,
                title: problem.Title,
                statusCode: problem.Status);
        }

        var csvText = await ReadCsv(file, ct);
        var result = await workflow.Import(csvText, ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ImportResultDto>, ProblemHttpResult>> CommitImportWithResolutions(
        [AsParameters] CommitCustomerImportFormDto form,
        [FromServices] CustomerImportWorkflowService workflow,
        CancellationToken ct)
    {
        if (!TryValidateImportFile(form.File, out var problem))
        {
            return TypedResults.Problem(
                detail: problem.Detail,
                title: problem.Title,
                statusCode: problem.Status);
        }

        var csvText = await ReadCsv(form.File, ct);
        var parsedConflictResolutions = ConflictResolutionSerialization.Parse(form.ConflictResolutions);
        var result = await workflow.Commit(
            csvText,
            parsedConflictResolutions,
            ct);

        return TypedResults.Ok(result);
    }

    private static async Task<string> ReadCsv(IFormFile file, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync(ct);
    }

    internal static bool TryValidateImportFile([NotNullWhen(true)] IFormFile? file, [NotNullWhen(false)] out ProblemDetails? problem)
    {
        if (file is null || file.Length <= 0 || file.Length > ContractConstants.CustomerImportMaxFileBytes || !IsAllowedCsv(file))
        {
            problem = CreateImportFileProblem();
            return false;
        }

        problem = null;
        return true;
    }

    private static bool IsAllowedCsv(IFormFile file)
    {
        var contentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType)
            ? mediaType.MediaType.Value
            : file.ContentType;

        return ContractConstants.CustomerImportAllowedContentTypes.Any(allowedContentType =>
                allowedContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            && Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private static ProblemDetails CreateImportFileProblem()
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid customer import file.",
            Detail = "Upload a CSV file that meets the documented import requirements."
        };
    }
}
